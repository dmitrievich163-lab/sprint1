namespace BookingsService.Domain;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Booking() { }

    public Booking(Guid eventId, Guid userId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        UserId = userId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        ProcessedAt = null;
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
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
