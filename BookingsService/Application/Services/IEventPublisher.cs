using SharedContracts;

namespace BookingsService.Application.Services;

public interface IEventPublisher
{
    Task PublishBookingConfirmedAsync(BookingConfirmed evt);
}
