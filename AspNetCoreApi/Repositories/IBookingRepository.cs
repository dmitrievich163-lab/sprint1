using AspNetCoreApi.Models;

namespace AspNetCoreApi.Repositories
{
    public interface IBookingRepository
    {
        Task<Guid> CreateBookingAsync(Guid eventId);
        Task<Booking?> GetByIdAsync(Guid bookingId);

        // Метод для фоновой обработки. Он инкапсулирует всю сложную логику транзакции.
        Task ProcessPendingBookingAsync(Guid bookingId);

        // Методы для явного управления статусом брони.
        Task RejectBookingAsync(Guid bookingId);
        Task ConfirmBookingAsync(Guid bookingId);
    }
}
