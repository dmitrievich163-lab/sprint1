using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class Booking
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid EventId { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ProcessedAt { get; private set; }

        public virtual User User { get; private set; } = null!;
        public virtual Event Event { get; set; } = null!;

        private Booking() { }

        public Booking(Guid eventId, Guid userId, User? user =null)
        {
            Id = Guid.NewGuid();
            EventId = eventId;
            Status = BookingStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            ProcessedAt = null;
            UserId = userId;

            User = user ?? null!;
        }


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

        public void Cancel()
        {
            switch (Status)
            {
                case BookingStatus.Cancelled:
                    throw new InvalidOperationException("Бронирование уже отменено ранее.");
                case BookingStatus.Confirmed:
                case BookingStatus.Rejected:
                    throw new InvalidOperationException($"Невозможно отменить бронь со статусом '{Status}'.");
                    // Статус Pending можно отменить
            }

            Status = BookingStatus.Cancelled;
            ProcessedAt = DateTime.UtcNow;
        }

    }
}
