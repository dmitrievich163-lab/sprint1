using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public class BookingService: IBookingService
    {
        private readonly InMemoryBookingRepository _repository;
        private readonly IEventService _eventService;
        public BookingService(InMemoryBookingRepository repository, IEventService eventService)
        {
            _repository = repository;
            _eventService = eventService;
        }

        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {
            _eventService.GetById(eventId);
            
            var booking = new Booking(eventId);
            _repository.Add(booking);

            return await Task.FromResult(booking.Id);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await Task.FromResult(_repository.GetById(bookingId));
        }
    }
}
