namespace EventsService.Domain;

public sealed class ProcessedBooking
{
    public Guid BookingId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
