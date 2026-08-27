namespace SharedContracts;

public sealed record BookingConfirmed
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int SeatCount { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
