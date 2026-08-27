namespace BookingsService.Application.Services;

public sealed class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }

    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0) return false;
        if (AvailableSeats >= count)
        {
            AvailableSeats -= count;
            return true;
        }
        return false;
    }
}

public interface IEventServiceClient
{
    Task<EventDto?> GetEventAsync(Guid eventId);
}
