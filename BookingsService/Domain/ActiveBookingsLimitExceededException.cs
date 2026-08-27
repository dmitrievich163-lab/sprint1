namespace BookingsService.Domain;

public class ActiveBookingsLimitExceededException : DomainException
{
    public ActiveBookingsLimitExceededException(int limit)
        : base($"Превышен лимит активных бронирований ({limit}).") { }
}
