using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;
using AspNetCoreApi.Services;
using k8s.Models;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace EventServices.Tests
{
    public class BookingServiceTests
    {
        private readonly EventService _eventService;
        private readonly InMemoryBookingRepository _repository;
        private readonly BookingService _bookingService;
        private readonly ITestOutputHelper _output;

        public BookingServiceTests(ITestOutputHelper output)
        {
            _eventService = new EventService();
            _repository = new InMemoryBookingRepository();
            _bookingService = new BookingService(_repository, _eventService);
            _output = output;
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_ExistingEvent_ReturnsPendingBooking()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;


            var bookingId =  await _bookingService.CreateBookingAsync(eventId);
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            Assert.NotNull(booking);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.NotNull(booking.CreatedAt);
        }

        [Fact]
        public async Task CreateBookings_SameEvent_UniqueIds()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            var id1 = await _bookingService.CreateBookingAsync(eventId);
            var id2 = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task GetBookingById_ExistingBooking_ReturnsCorrectInfo()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            var bookingId = await _bookingService.CreateBookingAsync(eventId);

            var result = await _bookingService.GetBookingByIdAsync(bookingId);

            Assert.NotNull(result);
            Assert.Equal(bookingId, result.Id);
        }

        [Fact]
        public async Task GetBooking_StatusChange_ReflectedInResult()
        {
            var eventId = Guid.NewGuid();
            var booking = new Booking(eventId);

            booking.Confirm();

            _repository.Add(booking);

            var result = await _bookingService.GetBookingByIdAsync(booking.Id);

            Assert.NotNull(result);
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt); // Поле должно быть заполнено
        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_Check()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;


            var bookingId = await _bookingService.CreateBookingAsync(eventId);

            var @event = _eventService.GetById(eventId);

            Assert.Equal(@event.AvailableSeats, AvailableSeats-1);
          
        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_limit()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;


            var bookingId1 = await _bookingService.CreateBookingAsync(eventId);
            var bookingId2 = await _bookingService.CreateBookingAsync(eventId);
            var bookingId3 = await _bookingService.CreateBookingAsync(eventId);

            var @event = _eventService.GetById(eventId);

            Assert.Equal(@event.AvailableSeats, AvailableSeats - 3);
            Assert.NotEqual(bookingId1, bookingId2);
            Assert.NotEqual(bookingId1, bookingId3);

        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_NoAvailableSeatsException()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 3 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;


            var bookingId1 = await _bookingService.CreateBookingAsync(eventId);
            var bookingId2 = await _bookingService.CreateBookingAsync(eventId);
            var bookingId3 = await _bookingService.CreateBookingAsync(eventId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventId);
            });


        }

        [Fact]
        public async Task CreateBooking_Status_Change()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            var bookingId = await _bookingService.CreateBookingAsync(eventId);
            var @booking = await _bookingService.GetBookingByIdAsync(bookingId);

            var @event = _eventService.GetById(eventId);

            Assert.Equal(0, @event.AvailableSeats);

            @booking.Reject();

            var newBooking = await _bookingService.GetBookingByIdAsync(bookingId);

            Assert.Equal(BookingStatus.Rejected, newBooking.Status);

            var newEvent2 = _eventService.GetById(eventId);
            newEvent2.ReleaseSeats();

            Assert.Equal(1, newEvent2.AvailableSeats);

            var bookingId2 = await _bookingService.CreateBookingAsync(eventId);

            var @event2 = _eventService.GetById(eventId);

            Assert.Equal(0, @event.AvailableSeats);

        }

        [Fact]
        public async Task CreateBooking_Concurrency_OnlyFiveBookingsCreated()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            const int totalRequests = 20;
            var tasks = new List<Task>();

            var successfulBookingIds = new ConcurrentBag<Guid>();
            var thrownExceptions = new ConcurrentBag<Exception>();

            for (int i = 0; i < totalRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var bookingId = await _bookingService.CreateBookingAsync(eventId);
                        successfulBookingIds.Add(bookingId);
                    }
                    catch (NoAvailableSeatsException ex)
                    {
                        thrownExceptions.Add(ex);
                    }
                    catch (Exception ex)
                    {
                        thrownExceptions.Add(ex);
                    }
                }));
            }

            await Task.WhenAll(tasks);


            Assert.Equal(5, successfulBookingIds.Count);

            Assert.Equal(15, thrownExceptions.Count(ex => ex is NoAvailableSeatsException));

            Assert.Empty(thrownExceptions.Except(thrownExceptions.OfType<NoAvailableSeatsException>()));

            var updatedEvent = _eventService.GetById(eventId);

            Assert.Equal(0, updatedEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBooking_Concurrency_AllBookingIdsAreUnique()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 10 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            const int totalRequests = 10;
            var tasks = new List<Task>();

            var createdBookingIds = new ConcurrentBag<Guid>();

            for (int i = 0; i < totalRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var bookingId = await _bookingService.CreateBookingAsync(eventId);
                        createdBookingIds.Add(bookingId);
                    }
                    catch (Exception ex)
                    {
                    }
                }));
            }

            await Task.WhenAll(tasks);


            Assert.Equal(10, createdBookingIds.Count);

            var uniqueIdsCount = createdBookingIds.Distinct().Count();

            Assert.Equal(createdBookingIds.Count, uniqueIdsCount);
        }


        // --- НЕУСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_NonExistentEvent_ThrowsExceptionOrReturnsError()
        {
             
            var eventId = Guid.NewGuid();


            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
             _bookingService.CreateBookingAsync(eventId)
        );

            Assert.Equal("Событие с ID "+eventId+" не найдено.", exception.Message);

        }

        [Fact]
        public async Task GetBooking_NonExistentBooking()
        {
            var nonExistentId = Guid.NewGuid();

            var result = await _bookingService.GetBookingByIdAsync(nonExistentId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetBooking_DeleteEvent()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 5 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            _eventService.Delete(eventId);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.CreateBookingAsync(eventId)
        );

            Assert.Equal("Событие с ID " + eventId + " не найдено.", exception.Message);
        }

        [Fact]
        public async Task CreateBooking_AvailableSeats_NoSeats()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;
            var AvailableSeats = createdEvent.AvailableSeats;

            await _bookingService.CreateBookingAsync(eventId);

            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            {
                await _bookingService.CreateBookingAsync(eventId);
            });


        }
    }
}
