using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public interface IBookingService
    {
        Task<Guid> CreateBookingAsync(Guid eventId);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    }
}
