using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public class InMemoryBookingRepository
    {
        private readonly List<Booking> _bookings = new();

        public void Add(Booking booking)
        {
            _bookings.Add(booking);
        }

        public Booking GetById(Guid id)
        {
            return _bookings.FirstOrDefault(b => b.Id == id);
        }

        public IEnumerable<Booking> GetAll()
        {
            return _bookings;
        }

        public IEnumerable<Booking> GetByEventId(Guid eventId)
        {
            return _bookings.Where(b => b.EventId == eventId);
        }
    }
}
