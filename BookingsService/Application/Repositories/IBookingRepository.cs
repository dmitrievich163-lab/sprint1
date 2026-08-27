using BookingsService.Domain;

namespace BookingsService.Application.Repositories;

public interface IBookingRepository
{
    Task<Guid> CreateBookingAsync(Guid eventId, Guid userId);
    Task<Booking?> GetByIdAsync(Guid bookingId);
    Task<IReadOnlyCollection<Booking>> GetActiveBookingsByUserIdAsync(Guid userId);
    Task ProcessPendingBookingAsync();
    Task RejectBookingAsync(Guid bookingId);
    Task ConfirmBookingAsync(Guid bookingId);
    Task CancelBookingAsync(Guid bookingId);
}
