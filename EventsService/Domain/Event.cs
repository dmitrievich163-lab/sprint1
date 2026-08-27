namespace EventsService.Domain;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
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

    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0) return;
        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }
}
