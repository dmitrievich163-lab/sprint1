using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using AspNetCoreApi.Services;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Collections.Concurrent;
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
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<AspNetCoreApi.Repositories.IEventRepository, AspNetCoreApi.Repositories.EventRepository>();
            services.AddScoped<AspNetCoreApi.Repositories.IBookingRepository, AspNetCoreApi.Repositories.BookingRepository>();
            services.AddScoped<IEventService, AspNetCoreApi.Services.EventService>();
            services.AddScoped<IBookingService, BookingService>();
            _serviceProvider = services.BuildServiceProvider();
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_ExistingEvent_ReturnsPendingBooking()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId);
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

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var id1 = await bookingService.CreateBookingAsync(eventId);
            var id2 = await bookingService.CreateBookingAsync(eventId);

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task GetBookingById_ExistingBooking_ReturnsCorrectInfo()
        {
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId);

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

            // Создаем событие для брони
            var createdEvent = await eventService.Create(new Event
            {
                Title = "Test Event",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 5
            });

            // Создаем новую бронь со статусом Pending
            var bookingId = await bookingService.CreateBookingAsync(createdEvent.Id);

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

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();

            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId = await bookingService.CreateBookingAsync(eventId);


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

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId1 = await bookingService.CreateBookingAsync(eventId);
            var bookingId2 = await bookingService.CreateBookingAsync(eventId);
            var bookingId3 = await bookingService.CreateBookingAsync(eventId);

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

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var bookingId1 = await bookingService.CreateBookingAsync(eventId);
            var bookingId2 = await bookingService.CreateBookingAsync(eventId);
            var bookingId3 = await bookingService.CreateBookingAsync(eventId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await bookingService.CreateBookingAsync(eventId);
            });


        }

        //[Fact]
        //public async Task CreateBooking_Status_Change()
        //{
        //    using var eventScope = _serviceProvider.CreateScope();
        //    var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

        //    var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
        //    var createdEvent = await eventService.Create(newEvent);
        //    var eventId = createdEvent.Id;
        //    var AvailableSeats = createdEvent.AvailableSeats;

        //    using var bookingScope = _serviceProvider.CreateScope();
        //    var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

        //    var bookingId = await bookingService.CreateBookingAsync(eventId);
        //    var @booking = await bookingService.GetBookingByIdAsync(bookingId);

        //    using var eventScope2 = _serviceProvider.CreateScope();
        //    var eventService2 = eventScope2.ServiceProvider.GetRequiredService<IEventService>();

        //    var @event = await eventService2.GetById(eventId);


        //    Assert.Equal(0, @event.AvailableSeats);

        //    using var bookingScope2 = _serviceProvider.CreateScope();
        //    var bookingService2 = bookingScope2.ServiceProvider.GetRequiredService<IBookingService>();

        //    await bookingService2.RejectBookingAsync(bookingId);

        //    var newBooking = await bookingService2.GetBookingByIdAsync(bookingId);

        //    Assert.Equal(BookingStatus.Rejected, newBooking.Status);

        //    using var eventScope3 = _serviceProvider.CreateScope();
        //    var eventService3 = eventScope3.ServiceProvider.GetRequiredService<IEventService>();

        //    var newEvent2 = await eventService3.GetById(eventId);
        //    newEvent2.ReleaseSeats();

        //    Assert.Equal(1, newEvent2.AvailableSeats);

        //    using var bookingScope3 = _serviceProvider.CreateScope();
        //    var bookingService3 = bookingScope3.ServiceProvider.GetRequiredService<IBookingService>();

        //    var bookingId2 = await bookingService3.CreateBookingAsync(eventId);

        //    using var eventScope4 = _serviceProvider.CreateScope();
        //    var eventService4 = eventScope4.ServiceProvider.GetRequiredService<IEventService>();

        //    var @event2 = await eventService4.GetById(eventId);

        //    Assert.Equal(0, @event.AvailableSeats);

        //}

        //[Fact]
        //public async Task CreateBooking_Concurrency_OnlyFiveBookingsCreated()
        //{
        //    // --- 1. ПОДГОТОВКА (ARANGE) ---
        //    using var setupScope = _serviceProvider.CreateScope();
        //    var eventService = setupScope.ServiceProvider.GetRequiredService<IEventService>();

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
        //            await bookingService.CreateBookingAsync(eventId);
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
                        var bookingId = await bookingService.CreateBookingAsync(eventId);
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


        // --- НЕУСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_NonExistentEvent_ThrowsExceptionOrReturnsError()
        {

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();


            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
             bookingService.CreateBookingAsync(eventId)
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
            using var eventScope = _serviceProvider.CreateScope();
            var eventService = eventScope.ServiceProvider.GetRequiredService<IEventService>();

            using var bookingScope = _serviceProvider.CreateScope();
            var bookingService = bookingScope.ServiceProvider.GetRequiredService<IBookingService>();

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            await eventService.Delete(eventId);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            bookingService.CreateBookingAsync(eventId)
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


            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
            var createdEvent = await eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            await bookingService.CreateBookingAsync(eventId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await bookingService.CreateBookingAsync(eventId);
            });


        }
    }
}

