namespace BookingsService.Domain;

public class BookingPolicy : IBookingPolicy
{
    private const int DefaultMaxActiveBookings = 5;

    public void CheckEventAvailability(DateTime eventDateUtc)
    {
        if (eventDateUtc <= DateTime.UtcNow)
        {
            throw new PastEventBookingException();
        }
    }

    public void CheckActiveBookingLimit(IReadOnlyCollection<Booking> activeBookings, int maxLimit = 5)
    {
        var limit = maxLimit > 0 ? maxLimit : DefaultMaxActiveBookings;

        if (activeBookings.Count >= limit)
        {
            throw new ActiveBookingsLimitExceededException(limit);
        }
    }

    public void CheckAccessRights(Guid currentUserId, bool isCurrentUserAdmin, Guid targetBookingOwnerId)
    {
        if (!isCurrentUserAdmin && currentUserId != targetBookingOwnerId)
        {
            throw new ForbiddenOperationException("изменение или просмотр чужой брони");
        }
    }
}
