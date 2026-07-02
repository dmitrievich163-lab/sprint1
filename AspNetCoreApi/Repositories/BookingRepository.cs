using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AspNetCoreApi.Exceptions;

namespace AspNetCoreApi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {
            var @event = await _context.Events.FindAsync(eventId);

            if (@event == null)
                throw new KeyNotFoundException($"Событие с ID {eventId} не найдено.");

            if (!@event.TryReserveSeats(1))
                throw new NoAvailableSeatsException("No available seats for this event.");

            var booking = new Booking(eventId);
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            return booking.Id;
        }

        public async Task<Booking?> GetByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        /// <summary>
        /// Атомарно обрабатывает бронь со статусом Pending.
        /// </summary>
        public async Task ProcessPendingBookingAsync(Guid bookingId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Используем FindAsync для эффективного поиска по первичному ключу.
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return; // Ничего не делаем, если бронь уже обработана или не существует.

                var @event = await _context.Events.FindAsync(booking.EventId);
                if (@event == null)
                {
                    booking.Reject(); // Если событие удалили, отклоняем бронь.
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return;
                }

                // Бизнес-логика внутри транзакции.
                if (@event.TryReserveSeats(1))
                {
                    booking.Confirm();
                }
                else
                {
                    booking.Reject();
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // В случае ошибки транзакция откатится автоматически при выходе из блока 'using'.
                // Логирование можно добавить здесь.
                throw; // Пробрасываем исключение дальше.
            }
        }

        public async Task RejectBookingAsync(Guid bookingId)
        {
            // Загружаем бронь вместе с событием, чтобы избежать лишних запросов к БД.
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
                return; // Игнорируем, если бронь уже в финальном состоянии.

            // Возвращаем место событию, если оно было занято.
            if (booking.Status == BookingStatus.Confirmed)
            {
                booking.Event?.ReleaseSeats(1);
            }

            booking.Reject();
            await _context.SaveChangesAsync();
        }

        public async Task ConfirmBookingAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");

            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException(
                    $"Невозможно подтвердить бронь со статусом {booking.Status}.");

            if (!booking.Event!.TryReserveSeats(1)) // ! так как Include гарантирует наличие
            {
                throw new NoAvailableSeatsException("Не удалось подтвердить бронь: закончились места.");
            }

            booking.Confirm();
            await _context.SaveChangesAsync();
        }
    }
}
