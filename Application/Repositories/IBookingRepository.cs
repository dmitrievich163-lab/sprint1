using Domain;

namespace Application.Repositories
{
    public interface IBookingRepository
    {
        Task<Guid> CreateBookingAsync(Guid eventId, Guid userId);
        Task<Booking?> GetByIdAsync(Guid bookingId);
        Task<Booking?> GetByIdWithEventAsync(Guid bookingId);
        // Метод для фоновой обработки. Он инкапсулирует всю сложную логику транзакции.
        Task ProcessPendingBookingAsync();

        // Методы для явного управления статусом брони.
        Task RejectBookingAsync(Guid bookingId);
        Task ConfirmBookingAsync(Guid bookingId);
    }
}
