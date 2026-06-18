using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public interface IBookingService
    {
        Task<Guid> CreateBookingAsync(Guid eventId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
        Task ProcessPendingBookingAsync(Guid bookingId);
        Task RejectBookingAsync(Guid bookingId);
        Task ConfirmBookingAsync(Guid bookingId);
    }
}
