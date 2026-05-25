using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public class BookingService: IBookingService
    {
        private readonly InMemoryBookingRepository _repository;
        private readonly IEventService _eventService;

        private readonly object _bookingLock = new();
        public BookingService(InMemoryBookingRepository repository, IEventService eventService)
        {
            _repository = repository;
            _eventService = eventService;
        }

        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {
            lock (_bookingLock)
            {
                var @event =_eventService.GetById(eventId);

                if (!@event.TryReserveSeats(1))
                {
                    throw new NoAvailableSeatsException("No available seats for this event.");
                }
                var booking = new Booking(eventId);
                _repository.Add(booking);

                _eventService.Update(eventId,@event);
                return  booking.Id;
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await Task.FromResult(_repository.GetById(bookingId));
        }
    }
}
