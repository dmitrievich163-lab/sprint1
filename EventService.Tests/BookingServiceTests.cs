using Application.Repositories;
using Application.Services;
using Domain;
using Infrastructure;
using Infrastructure.DataAccess;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Claims;
using Xunit.Abstractions;

namespace EventServices.Tests
{
    public class BookingServiceTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestOutputHelper _output;

        public BookingServiceTests(ITestOutputHelper output)
        {
            _output = output;
            var dbName = Guid.NewGuid().ToString(); // Уникальная БД для каждого запуска тестов

            var services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<Application.Repositories.IEventRepository, Application.Repositories.EventRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<Application.Repositories.IBookingRepository, BookingRepository>();
            services.AddScoped<IEventService, Application.Services.EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IBookingPolicy, BookingPolicy>();
            _serviceProvider = services.BuildServiceProvider();
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_ExistingEvent_ReturnsPendingBooking()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.UtcNow.AddHours(1), EndAt = DateTime.UtcNow.AddHours(5), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId, userId);
            var booking = await bookingService.GetBookingByIdAsync(bookingId);

            Assert.NotNull(booking);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.NotNull(booking.CreatedAt);
        }

        [Fact]
        public async Task CreateBookings_SameEvent_UniqueIds()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var id1 = await bookingService.CreateBookingAsync(eventId, userId);
            var id2 = await bookingService.CreateBookingAsync(eventId, userId);

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task GetBookingById_ExistingBooking_ReturnsCorrectInfo()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();
            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId, userId);

            var result = await bookingService.GetBookingByIdAsync(bookingId);

            Assert.NotNull(result);
            Assert.Equal(bookingId, result.Id);
        }

        [Fact]
        public async Task GetBooking_StatusChange_ReflectedInResult()
        {
            using var scope = _serviceProvider.CreateScope();

            // Получаем сервисы из контейнера
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;

            // Создаем событие для брони
            var createdEvent = await eventService.Create(new Event
            {
                Title = "Test Event",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 5
            });

            // Создаем новую бронь со статусом Pending
            var bookingId = await bookingService.CreateBookingAsync(createdEvent.Id, userId);

            var booking = await bookingService.GetBookingByIdAsync(bookingId);
            await bookingService.ConfirmBookingAsync(bookingId);

            // Act: Вызываем метод подтверждения через сервис

            // Assert: Проверяем результат, снова обратившись к сервису
            var confirmedBooking = await bookingService.GetBookingByIdAsync(bookingId);

            // Проверки
            Assert.NotNull(confirmedBooking);
            Assert.Equal(BookingStatus.Confirmed, confirmedBooking.Status); // Статус изменился на Confirmed
            Assert.NotNull(confirmedBooking.ProcessedAt); // Поле ProcessedAt заполнено
            Assert.True(confirmedBooking.ProcessedAt <= DateTime.UtcNow); // Время обработки установлено корректно

        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_Check()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();

            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId, userId);


            using var eventScope2 = _serviceProvider.CreateScope();
            var eventService2 = eventScope2.ServiceProvider.GetRequiredService<IEventService>();

            var @event = await eventService2.GetById(eventId);

            Assert.Equal(@event.AvailableSeats, AvailableSeats - 1);

        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_limit()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId1 = await bookingService.CreateBookingAsync(eventId, userId);
            var bookingId2 = await bookingService.CreateBookingAsync(eventId, userId);
            var bookingId3 = await bookingService.CreateBookingAsync(eventId, userId);

            using var eventScope2 = _serviceProvider.CreateScope();
            var eventService2 = eventScope2.ServiceProvider.GetRequiredService<IEventService>();

            var @event = await eventService2.GetById(eventId);

            Assert.Equal(@event.AvailableSeats, AvailableSeats - 3);
            Assert.NotEqual(bookingId1, bookingId2);
            Assert.NotEqual(bookingId1, bookingId3);

        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_NoAvailableSeatsException()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create(
        login: "testuser@example.com",
        passwordHash: PasswordHash.CreateFromPlainText("qwerty"),
        role: UserRole.User);
            var userId = user.Id;

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId1 = await bookingService.CreateBookingAsync(eventId, userId);
            var bookingId2 = await bookingService.CreateBookingAsync(eventId, userId);
            var bookingId3 = await bookingService.CreateBookingAsync(eventId, userId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await bookingService.CreateBookingAsync(eventId, userId);
            });
        }

        [Fact]
        public async Task CreateBooking_PastEvent_ThrowsPastEventBookingException()
        {
            // Arrange
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            // Событие в прошлом
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var @event = new Event
            {
                Title = "Old Concert",
                StartAt = pastDate,
                EndAt = pastDate.AddHours(2),
                TotalSeats = 10,
                AvailableSeats = 10
            };
            var user = Domain.User.Create("test@example.com", PasswordHash.CreateFromPlainText("qwerty"), UserRole.User);
            var userId = user.Id;
            var createdEvent = await eventService.Create(@event);
            var eventId = createdEvent.Id;
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<PastEventBookingException>(async () =>
            {
                await bookingService.CreateBookingAsync(@event.Id, user.Id);
            });

            Assert.NotNull(exception);
        }

        [Fact]
        public async Task CreateBooking_ExceedsUserLimit_ThrowsActiveBookingsLimitExceededException()
        {
            // Arrange
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var futureDate = DateTime.UtcNow.AddDays(1);
            var @event = new Event { Title = "Concert", StartAt = futureDate, EndAt = futureDate.AddHours(2), TotalSeats = 100, AvailableSeats = 100 };
            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;
            var createdEvent = await eventService.Create(@event);
            var eventId = createdEvent.Id;
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();


            // Создаем максимально допустимое количество броней (в политике стоит DefaultMaxActiveBookings = 5)
            for (int i = 0; i < 10; i++)
            {
                await bookingService.CreateBookingAsync(@event.Id, user.Id);
            }

            // Act & Assert: Шестая бронь должна упасть
            var exception = await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(async () =>
            {
                await bookingService.CreateBookingAsync(@event.Id, user.Id);
            });

            Assert.Equal(10, exception.Limit); // Проверяем, что сообщение об ошибке содержит правильный лимит
        }

        [Fact]
        public async Task CreateBooking_UserALimitReached_UserBCanStillBook()
        {
            // Arrange
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var futureDate = DateTime.UtcNow.AddDays(1);
            var @event = new Event { Title = "Big Concert", StartAt = futureDate, EndAt = futureDate.AddHours(2), TotalSeats = 100, AvailableSeats = 100 };

            // Пользователь №1 (исчерпает лимит)
            var userA = Domain.User.Create("userA@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);
            // Пользователь №2 (свободен)
            var userB = Domain.User.Create("userB@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId1 = userA.Id;
            var userId2 = userB.Id;
            var createdEvent = await eventService.Create(@event);
            var eventId = createdEvent.Id;
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();


            // Пользователь A делает 5 броней (до лимита)
            for (int i = 0; i < 10; i++)
            {
                await bookingService.CreateBookingAsync(@event.Id, userA.Id);
            }

            // Act: Пользователь B пытается сделать свою ПЕРВУЮ бронь
            var bookingIdForUserB = await bookingService.CreateBookingAsync(@event.Id, userB.Id);

            // Assert
            Assert.NotEqual(Guid.Empty, bookingIdForUserB);

            // Дополнительная проверка: убеждаемся, что пользователь A всё еще заблокирован
            var ex = await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(async () =>
            {
                await bookingService.CreateBookingAsync(@event.Id, userA.Id);
            });
        }


        [Fact]
        public async Task CreateBooking_Status_Change()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId, userId);
            var @booking = await bookingService.GetBookingByIdAsync(bookingId);

            using var eventScope2 = _serviceProvider.CreateScope();
            var eventService2 = eventScope2.ServiceProvider.GetRequiredService<IEventService>();

            var @event = await eventService2.GetById(eventId);


            Assert.Equal(0, @event.AvailableSeats);

            using var bookingScope2 = _serviceProvider.CreateScope();
            var bookingService2 = bookingScope2.ServiceProvider.GetRequiredService<IBookingService>();

            await bookingService2.RejectBookingAsync(bookingId);

            var newBooking = await bookingService2.GetBookingByIdAsync(bookingId);

            Assert.Equal(BookingStatus.Rejected, newBooking.Status);

            using var eventScope3 = _serviceProvider.CreateScope();
            var eventService3 = eventScope3.ServiceProvider.GetRequiredService<IEventService>();

            var newEvent2 = await eventService3.GetById(eventId);
            newEvent2.ReleaseSeats();

            await eventService3.Update(newEvent2.Id, newEvent2);

            Assert.Equal(1, newEvent2.AvailableSeats);

            using var bookingScope3 = _serviceProvider.CreateScope();
            var bookingService3 = bookingScope3.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId2 = await bookingService3.CreateBookingAsync(eventId, userId);

            using var eventScope4 = _serviceProvider.CreateScope();
            var eventService4 = eventScope4.ServiceProvider.GetRequiredService<IEventService>();

            var @event2 = await eventService4.GetById(eventId);

            Assert.Equal(0, @event.AvailableSeats);

        }

        //[Fact]
        //public async Task CreateBooking_Concurrency_OnlyFiveBookingsCreated()
        //{
        //    // --- 1. ПОДГОТОВКА (ARANGE) ---
        //    using var setupScope = _serviceProvider.CreateScope();
        //    var eventService = setupScope.ServiceProvider.GetRequiredService<IEventService>();

        //    var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

        //    var userId = user.Id;

        //    var newEvent = new Event
        //    {
        //        Title = "Test Event",
        //        StartAt = DateTime.UtcNow,
        //        EndAt = DateTime.UtcNow.AddHours(1),
        //        TotalSeats = 5,
        //        AvailableSeats = 5 // Обязательно инициализируем!
        //    };
        //    var createdEvent = await eventService.Create(newEvent);
        //    var eventId = createdEvent.Id;


        //    // --- 2. ДЕЙСТВИЕ (ACT) ---
        //    const int totalRequests = 20;
        //    var tasks = new List<Task>();

        //    for (int i = 0; i < totalRequests; i++)
        //    {
        //        tasks.Add(Task.Run(async () =>
        //        {
        //            using var taskScope = _serviceProvider.CreateScope();
        //            var bookingService = taskScope.ServiceProvider.GetRequiredService<IBookingService>();

        //            // Нам больше не нужно ловить исключения здесь.
        //            // Метод CreateBookingAsync сам справится с логикой.
        //            await bookingService.CreateBookingAsync(eventId, userId);
        //        }));
        //    }

        //    // Ждем завершения всех задач.
        //    // Ожидаем, что часть из них упадет с DbUpdateException или другими ошибками,
        //    // но это нас больше не волнует.
        //    await Task.WhenAll(tasks);

        //    // --- 3. ПРОВЕРКА (ASSERT) ---
        //    // Используем ЕЩЕ ОДИН новый scope для финальной проверки состояния БД.
        //    using var finalCheckScope = _serviceProvider.CreateScope();
        //    var dbContext = finalCheckScope.ServiceProvider.GetRequiredService<AppDbContext>();

        //    // Проверка 1: В таблице Bookings должно быть ровно 5 записей для нашего события.
        //    int bookingsCountInDb = await dbContext.Bookings.CountAsync(b => b.EventId == eventId);
        //    Assert.Equal(5, bookingsCountInDb);

        //    // Проверка 2: У события должно быть 0 оставшихся мест.
        //    var updatedEvent = await dbContext.Events.FindAsync(eventId);
        //    Assert.Equal(0, updatedEvent.AvailableSeats);
        //}

        [Fact]
        public async Task CreateBooking_Concurrency_AllBookingIdsAreUnique()
        {
            // 1. ARRANGE: Подготовка. Создаем событие в отдельном скоупе.
            using var setupScope = _serviceProvider.CreateScope();
            var eventService = setupScope.ServiceProvider.GetRequiredService<IEventService>();

            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;
            var newEvent = new Event
            {
                Title = "Test Event",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 10 // Ставим 10, чтобы все запросы прошли успешно
            };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            // 2. ACT: Действие. Запускаем параллельные задачи.
            const int totalRequests = 10;
            var tasks = new List<Task>();
            var createdBookingIds = new ConcurrentBag<Guid>(); // Коллекция для безопасного добавления из разных потоков

            for (int i = 0; i < totalRequests; i++)
            {
                // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ:
                // Создаем НОВЫЙ scope для КАЖДОЙ задачи.
                // Это симулирует независимый запрос (например, от разных пользователей).
                tasks.Add(Task.Run(async () =>
                {
                    using var taskScope = _serviceProvider.CreateScope(); // <-- НОВЫЙ SCOPE ЗДЕСЬ!
                    var bookingService = taskScope.ServiceProvider.GetRequiredService<IBookingService>();

                    try
                    {
                        // Каждый запрос использует свой DbContext
                        var bookingId = await bookingService.CreateBookingAsync(eventId, userId);
                        createdBookingIds.Add(bookingId);
                    }
                    catch (Exception ex)
                    {
                        // В данном тесте мы не ожидаем ошибок, но логируем их на всякий случай.
                        // Можно использовать ITestOutputHelper для вывода в лог теста.
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // 3. ASSERT: Проверка результатов.

            // Проверяем, что все 10 запросов создали бронь успешно.
            Assert.Equal(totalRequests, createdBookingIds.Count);

            // Проверяем, что все ID уникальны.
            // Distinct() уберет дубликаты, если бы они были.
            var uniqueIdsCount = createdBookingIds.Distinct().Count();

            Assert.Equal(createdBookingIds.Count, uniqueIdsCount);
        }


        //        // --- НЕУСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_NonExistentEvent_ThrowsExceptionOrReturnsError()
        {
            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();


            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
             bookingService.CreateBookingAsync(eventId, userId)
        );

            Assert.Equal("Событие с ID " + eventId + " не найдено.", exception.Message);

        }

        [Fact]
        public async Task GetBooking_NonExistentBooking()
        {
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var nonExistentId = Guid.NewGuid();

            var result = await bookingService.GetBookingByIdAsync(nonExistentId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetBooking_DeleteEvent()
        {
            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;

            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            await eventService.Delete(eventId);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            bookingService.CreateBookingAsync(eventId, userId)
        );

            Assert.Equal("Событие с ID " + eventId + " не найдено.", exception.Message);
        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_NoSeats()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var user = Domain.User.Create("limit@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var userId = user.Id;
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            await bookingService.CreateBookingAsync(eventId, userId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await bookingService.CreateBookingAsync(eventId, userId);
            });


        }

        [Fact]
        public async Task CancelBooking_OtherUsersBooking_AdminOnly_ReturnsForbidden()
        {
            // Arrange
            await using var scope = _serviceProvider.CreateAsyncScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            // Создаем двух разных пользователей
            var owner = Domain.User.Create("owner@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);
            var otherUser = Domain.User.Create("hacker@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);

            var @event = new Event { Title = "Concert", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(5), TotalSeats = 5 };
            var createdEvent = await eventService.Create(@event);
            var eventId = createdEvent.Id;
            // Создаем бронь от имени OWNER
            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();
            var bookingId = await bookingService.CreateBookingAsync(@event.Id, owner.Id);

            // ACT: Пытаемся отменить как OTHER USER
            await using var hackerScope = _serviceProvider.CreateAsyncScope();

            // Эмуляция того, что текущий HttpContext принадлежит HACKER'у
            SetupCurrentUser(hackerScope, otherUser.Id, isAdmin: false);

            var hackerBookingService = hackerScope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenOperationException>(async () =>
            {
                await hackerBookingService.CancelBookingAsync(bookingId);
            });

            Assert.Contains("Недостаточно прав для выполнения операции", ex.Message);
        }

        [Fact]
        public async Task CancelBooking_AdminCanCancelOthersBooking_Success()
        {
            // Arrange
            await using var arrangeScope = _serviceProvider.CreateAsyncScope();
            var eventService = arrangeScope.ServiceProvider.GetRequiredService<IEventService>();

            var owner = Domain.User.Create("owner@test.com", PasswordHash.CreateFromPlainText("pass"), UserRole.User);
            var adminUser = Domain.User.Create("admin@test.com", PasswordHash.CreateFromPlainText("admin"), UserRole.Admin);

            var @event = new Event
            {
                Title = "Concert",
                StartAt = DateTime.UtcNow.AddHours(1),
                EndAt = DateTime.UtcNow.AddHours(5),
                TotalSeats = 5,
                AvailableSeats = 5
            };
            await eventService.Create(@event);
            var eventId = @event.Id;

            // Создаем бронь от имени OWNER
            await using var bookingScope = _serviceProvider.CreateAsyncScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();
            var bookingId = await bookingService.CreateBookingAsync(eventId, owner.Id);

            // ACT: Админ пытается отменить ЧУЖУЮ бронь
            await using var hackerScope = _serviceProvider.CreateAsyncScope();

            SetupCurrentUser(hackerScope, userId: adminUser.Id, isAdmin: true); // Эмулируем АДМИНА

            var hackerBookingService = hackerScope.ServiceProvider.GetRequiredService<IBookingService>();

            // Act: Просто вызываем метод. ОШИБКИ БЫТЬ НЕ ДОЛЖНО.
            await hackerBookingService.CancelBookingAsync(bookingId);

            // Assert: Проверяем состояние в новой транзакции/контексте
            await using var newBooking = _serviceProvider.CreateAsyncScope();
            var bookingService1 = newBooking.ServiceProvider.GetRequiredService<IBookingService>();
            var updatedBooking = await bookingService1.GetBookingByIdAsync(bookingId);
            
            // 1. Бронь должна существовать
            Assert.NotNull(updatedBooking);

            // 2. Статус должен измениться на Cancelled
            Assert.Equal(BookingStatus.Cancelled, updatedBooking.Status);

            await using var newEventScope = _serviceProvider.CreateAsyncScope();
            var eventServiceNew = newEventScope.ServiceProvider.GetRequiredService<IEventService>();
            var updatedEvent = await eventServiceNew.GetById(eventId);
            // 3. Места должны вернуться событию (так как статус был Pending или Confirmed)
            // В данном случае Created -> Pending, значит при отмене место возвращается
            Assert.Equal(5, updatedEvent.AvailableSeats);
        }
        private void SetupCurrentUser(IServiceScope scope, Guid userId, bool isAdmin)
        {
            // Получаем экземпляр IHttpContextAccessor из контейнера этого скоупа
            var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            // Создаем Claims (паспорт юзера)
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, isAdmin ? UserRole.Admin.ToString() : UserRole.User.ToString())
    };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Подменяем HttpContext в Accessor'е
            accessor.HttpContext = new DefaultHttpContext
            {
                User = principal
            };
        }
    }
}

    