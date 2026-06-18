using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApi.Services
{
    public class BookingService : IBookingService
    {
        //private readonly InMemoryBookingRepository _repository;
        //private readonly IEventService _eventService;

        private readonly AppDbContext _context;

        private static readonly SemaphoreSlim _bookingLock = new(1, 1);
        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {
            await _bookingLock.WaitAsync();

            try
            {
                var @event = await _context.Events.FindAsync(eventId);

                if (@event == null)
                {
                    throw new KeyNotFoundException($"Событие с ID {eventId} не найдено.");
                }

                if (!@event.TryReserveSeats(1))
                {
                    throw new NoAvailableSeatsException("No available seats for this event.");
                }
                var booking = new Booking(eventId);
                await _context.Bookings.AddAsync(booking);

                await _context.SaveChangesAsync();

                return booking.Id;
            }
            finally
            {
                // Обязательно освобождаем семафор, чтобы другие запросы могли выполняться.
                _bookingLock.Release();
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        public async Task ProcessPendingBookingAsync(Guid bookingId)
        {
            // Используем явную транзакцию для гарантии атомарности.
            // Если что-то пойдет не так (например, не хватит мест), все изменения откатятся.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Находим бронь и событие в одном контексте.
                // Используем FindAsync для эффективного поиска по ключу.
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null || booking.Status != BookingStatus.Pending)
                {
                    // Бронь уже обработана или не существует. Ничего не делаем.
                    return;
                }

                var @event = await _context.Events.FindAsync(booking.EventId);
                if (@event == null)
                {
                    // Если событие удалили, просто отклоняем бронь.
                    booking.Reject();
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return;
                }

                // 2. Выполняем бизнес-логику.
                // Метод TryReserveSeats может вернуть false, если мест не хватает.
                if (@event.TryReserveSeats(1))
                {
                    // Мест хватило, подтверждаем бронь.
                    booking.Confirm();
                }
                else
                {
                    // Мест не хватило, отклоняем бронь.
                    booking.Reject();
                }

                // 3. Сохраняем изменения.
                // EF Core увидит изменения и в @event (AvailableSeats), и в booking (Status).
                await _context.SaveChangesAsync();

                // 4. Если все прошло успешно, подтверждаем транзакцию.
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // Если произошла ошибка, транзакция автоматически откатится при выходе из using-блока.
                // Можно добавить логирование ошибки здесь.
                throw; // Или просто return, в зависимости от требований к отказоустойчивости.
            }
        }
        public async Task RejectBookingAsync(Guid bookingId)
        {
            // 1. Находим бронь вместе с связанным событием (Eager Loading).
            // Это позволяет избежать лишних запросов к БД и работать с объектом события напрямую.
            var booking = await _context.Bookings
                .Include(b => b.Event) // Обязательно включаем навигационное свойство
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");
            }

            // Проверяем статус, если это необходимо по бизнес-логике.
            // Например, можно отклонять только те брони, которые еще не были обработаны.
            if (booking.Status != BookingStatus.Pending)
            {
                // Или просто проигнорировать, или выбросить исключение.
                // Здесь мы просто игнорируем уже подтвержденные/отклоненные брони.
                return;
            }

            // 2. Выполняем бизнес-логику.
            // Сначала возвращаем место событию.
            // Мы проверяем на null для безопасности, хотя Include должен был загрузить объект.
            booking.Event?.ReleaseSeats(1);

            // Затем меняем статус самой брони.
            booking.Reject();

            // 3. Сохраняем все изменения одним запросом.
            // EF Core сам поймет, что нужно обновить и таблицу Bookings, и таблицу Events.
            await _context.SaveChangesAsync();
        }
    
    public async Task ConfirmBookingAsync(Guid bookingId)
        {
            // 1. Находим бронь вместе с связанным событием (Eager Loading).
            var booking = await _context.Bookings
                .Include(b => b.Event) // Загружаем событие, чтобы работать с ним напрямую
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");
            }

            // Проверяем, можно ли подтвердить эту бронь.
            // Обычно подтверждают только те, которые еще находятся на рассмотрении.
            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Невозможно подтвердить бронь со статусом {booking.Status}.");
            }

            // 2. Выполняем бизнес-логику.

            // Сначала резервируем место в событии.
            // Если мест нет, метод TryReserveSeats вернет false.
            if (!booking.Event.TryReserveSeats(1))
            {
                // В зависимости от вашей бизнес-логики, здесь можно выбросить исключение,
                // если подтверждение должно быть гарантированным.
                throw new NoAvailableSeatsException("Не удалось подтвердить бронь: закончились места.");
            }

            // Затем меняем статус самой брони.
            booking.Confirm();

            // 3. Сохраняем все изменения одним запросом.
            // EF Core обновит таблицы Bookings и Events.
            await _context.SaveChangesAsync();
        }
    } 
}

