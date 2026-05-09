using AspNetCoreApi.Models;
using AspNetCoreApi.Services;
using k8s.Models;

namespace EventServices.Tests
{
    public class BookingServiceTests
    {
        private readonly EventService _eventService;
        private readonly InMemoryBookingRepository _repository;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _eventService = new EventService();
            _repository = new InMemoryBookingRepository();
            _bookingService = new BookingService(_repository, _eventService);
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task CreateBooking_ExistingEvent_ReturnsPendingBooking()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };
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
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            var id1 = await _bookingService.CreateBookingAsync(eventId);
            var id2 = await _bookingService.CreateBookingAsync(eventId);

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task GetBookingById_ExistingBooking_ReturnsCorrectInfo()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };
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
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };
            var createdEvent = _eventService.Create(newEvent);
            var eventId = createdEvent.Id;

            _eventService.Delete(eventId);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.CreateBookingAsync(eventId)
        );

            Assert.Equal("Событие с ID " + eventId + " не найдено.", exception.Message);
        }
    }
}
