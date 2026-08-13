using Application.Repositories;
using Application.Services;
using Domain;
using Infrastructure;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventServices.Tests
{
    public class BookingRepositoryTest : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

        private string? _connectionString;
        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
            await using var setupContext = CreateContextForMigration();

            await setupContext.Database.MigrateAsync();
        }

        private AppDbContext CreateContextForMigration()
        {
            if (_connectionString == null)
                throw new InvalidOperationException("Connection string is not initialized.");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                 .UseNpgsql(_connectionString)
                 .Options;

            return new AppDbContext(options);
        }


        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Bookings\", \"Events\" RESTART IDENTITY CASCADE");
        }
        [Fact]
        public async Task CreateBookingAsync_CreatesBookingAndDecreasesEventSeats()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            // Создаем событие с 10 местами
            var @event = new Event { Title = "Тестовый концерт", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = 10, AvailableSeats = 10 };
            var user = User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User
    );
            await context.Events.AddAsync(@event);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);
            var eventRepository = new EventRepository(context);
            var userRepository = new UserRepository(context);
            var bookingPolicy = new BookingPolicy();
            var bookingService = new BookingService(
        bookingRepository,
        eventRepository,
        userRepository,
        bookingPolicy
    );
            var bookingServise = new BookingService(bookingRepository, eventRepository, userRepository
                , bookingPolicy);
            var eventService = new EventService(eventRepository); // Предполагаемое имя сервиса

            // Act
            var newBookingId = await bookingServise.CreateBookingAsync(@event.Id, user.Id);

            // Assert: Проверяем, что ID сгенерирован
            Assert.NotEqual(Guid.Empty, newBookingId);

            // Assert: Проверяем состояние в БД через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(newBookingId);
            var eventFromDb = await verificationContext.Events.FindAsync(@event.Id);

            Assert.NotNull(bookingFromDb);
            Assert.Equal(@event.Id, bookingFromDb.EventId);
            Assert.Equal(BookingStatus.Pending, bookingFromDb.Status);

            Assert.NotNull(eventFromDb);
            Assert.Equal(9, eventFromDb.AvailableSeats); // Проверяем, что место зарезервировано
        }

        [Fact]
        public async Task ProcessPendingBookingAsync_ConfirmsWhenSeatsAvailable()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Тестовый концерт", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = 10, AvailableSeats = 1 };
            var user = User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);

            await context.Events.AddAsync(@event);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Создаем бронь в статусе Pending
            var pendingBooking = new Booking(@event.Id,user.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);
            var eventRepository = new EventRepository(context);
            var userRepository = new UserRepository(context);
            var bookingPolicy = new BookingPolicy();
            var bookingServise = new BookingService(bookingRepository,
        eventRepository,
        userRepository,
        bookingPolicy);
            var eventService = new EventService(eventRepository);
            // Act
            await bookingServise.ProcessPendingBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);
            var eventFromDb = await verificationContext.Events.FindAsync(@event.Id);

            Assert.Equal(BookingStatus.Confirmed, bookingFromDb.Status);
            Assert.Equal(0, eventFromDb.AvailableSeats); // Место окончательно занято
        }

        [Fact]
        public async Task ProcessPendingBookingAsync_RejectsWhenNoSeatsAvailable()
        {
            // Arrange: Создаем событие без мест
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Тестовый концерт", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = 0, AvailableSeats = 0 };
            var user = User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);

            await context.Events.AddAsync(@event);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var pendingBooking = new Booking(@event.Id, user.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);
            var eventRepository = new EventRepository(context);
            var userRepository = new UserRepository(context);
            var bookingPolicy = new BookingPolicy();
            var bookingServise = new BookingService(bookingRepository,
        eventRepository,
        userRepository,
        bookingPolicy);
            var eventService = new EventService(eventRepository);

            // Act
            await bookingServise.ProcessPendingBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);

            Assert.Equal(BookingStatus.Rejected, bookingFromDb.Status);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingBooking_ReturnsEntityWithCorrectData()
        {
            // Arrange: Подготавливаем данные напрямую в БД
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var user = User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);

            var @event = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Оригинальный Концерт",
                Description = "Описание",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(3),
                AvailableSeats = 100,
            };
            await context.Events.AddAsync(@event);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var repository = new BookingRepository(context);

            // Act: Вызываем метод из-под другого контекста (как это было бы в рантайме приложения)
            await using var newContext = CreateContext();
            var repoForAct = new BookingRepository(newContext);

            var booking = await repoForAct.CreateBookingAsync(@event.Id, user.Id);

            await using var newContext2 = CreateContext();
            var repoForAct2 = new BookingRepository(newContext2);

            var result = await repoForAct.GetByIdAsync(booking);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking, result!.Id);
            Assert.Equal(result.Status, BookingStatus.Pending);
            Assert.Equal(result.EventId, @event.Id);
        }


        [Fact]
        public async Task RejectBookingAsync_RejectsPendingBooking()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var user = User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);

            var @event = new Event { Title = "Концерт", TotalSeats = 10, AvailableSeats = 10 };
            await context.Events.AddAsync(@event);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var pendingBooking = new Booking(@event.Id,user.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);
            var eventRepository = new EventRepository(context);
            var userRepository = new UserRepository(context);
            var bookingPolicy = new BookingPolicy();
            var bookingServise = new BookingService(bookingRepository,
        eventRepository,
        userRepository,
        bookingPolicy);
            var eventService = new EventService(eventRepository);

            // Act
            await bookingServise.RejectBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);

            Assert.Equal(BookingStatus.Rejected, bookingFromDb.Status);
        }
}
}
