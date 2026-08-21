using Domain;

namespace Application.Services
{
    public interface IBookingService
    {
        Task<Guid> CreateBookingAsync(Guid eventId, Guid userId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
        Task ProcessPendingBookingAsync(Guid bookingId);
        Task RejectBookingAsync(Guid bookingId);
        Task ConfirmBookingAsync(Guid bookingId);
        Task CancelBookingAsync(Guid bookingId);
    }
}
