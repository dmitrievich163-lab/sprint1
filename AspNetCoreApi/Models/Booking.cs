using AspNetCoreApi.Services;

namespace AspNetCoreApi.Models
{
    public class Booking
    {
        public Guid Id { get; private set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }

        private Booking()
        {
            // Инициализируем коллекцию, если бы она была здесь
            // Навигационное свойство Event может быть null, поэтому используем !
            Event = null!;
        }

        public Booking(Guid eventId)
        {
            Id = Guid.NewGuid();
            EventId = eventId;
            Status = BookingStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            ProcessedAt = null;
        }

        public virtual Event Event { get; set; } = null!;

        public void Confirm()
        {
            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;

        }
    }
}
