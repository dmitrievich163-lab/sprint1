using Application.Repositories;
using Domain;
using System.Transactions;

namespace Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;

        // Конструктор принимает интерфейс репозитория, а не DbContext
        public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }

        // Все методы просто перенаправляют вызов в репозиторий
        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {

            var @event = await _eventRepository.GetByIdAsync(eventId);

            if (@event == null)
                throw new KeyNotFoundException($"Событие с ID {eventId} не найдено.");

            if (!@event.TryReserveSeats(1))
                throw new NoAvailableSeatsException("No available seats for this event.");

            return await _bookingRepository.CreateBookingAsync(eventId);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _bookingRepository.GetByIdAsync(bookingId);
        }

        public async Task ProcessPendingBookingAsync(Guid bookingId)
        {
            try
            {
                // Используем FindAsync для эффективного поиска по первичному ключу.
                var booking = await _bookingRepository.GetByIdWithEventAsync(bookingId);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return; // Ничего не делаем, если бронь уже обработана или не существует.

                if (booking.Event == null)
                {
                    booking.Reject(); // Если событие удалили, отклоняем бронь.
                    
                }

                // Бизнес-логика внутри транзакции.
                if (booking.Event.TryReserveSeats(1))
                {
                    booking.Confirm();
                }
                else
                {
                    booking.Reject();
                }

                await _bookingRepository.ProcessPendingBookingAsync();
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
            var booking = await _bookingRepository.GetByIdWithEventAsync(bookingId);

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
            await _bookingRepository.RejectBookingAsync(bookingId);
        }

        public async Task ConfirmBookingAsync(Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdWithEventAsync(bookingId);

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
            await _bookingRepository.ConfirmBookingAsync(bookingId);
        }
    }
}

