using Application.Repositories;
using Domain;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Transactions;


namespace Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBookingPolicy _policy;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Конструктор принимает интерфейс репозитория, а не DbContext
        public BookingService(IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            IUserRepository userRepository,
            IBookingPolicy policy,
            IHttpContextAccessor httpContextAccessor)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _userRepository = userRepository;
            _policy = policy;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid GetCurrentUserId()
        {
            var idString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idString))
                throw new UnauthorizedAccessException("Не удалось определить пользователя.");

            return Guid.Parse(idString);
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(UserRole.Admin.ToString()) ?? false;
        }

        // Все методы просто перенаправляют вызов в репозиторий
        public async Task<Guid> CreateBookingAsync(Guid eventId, Guid userId)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);

            if (@event == null)
                throw new KeyNotFoundException($"Событие с ID {eventId} не найдено.");
            if (!@event.TryReserveSeats(1))
                throw new NoAvailableSeatsException("No available seats for this event.");
            _policy.CheckEventAvailability(@event.StartAt);

            // Проверка: лимит активных броней у пользователя
            var activeBookings = await _userRepository.GetActiveBookingsByUserIdAsync(userId);
            _policy.CheckActiveBookingLimit(activeBookings, 10); // Лимит строго 10

            // Если все ок — создаем бронь с привязкой к пользователю
            return await _bookingRepository.CreateBookingAsync(eventId, userId);
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
        public async Task CancelBookingAsync(Guid bookingId)
        {
            var currentUserId = GetCurrentUserId();
            bool isAdmin = IsAdmin();

            var booking = await _bookingRepository.GetByIdWithEventAsync(bookingId);
            if (booking == null)
                throw new KeyNotFoundException($"Бронирование {bookingId} не найдено.");

            // === ПРОВЕРКА ПРАВ ДОСТУПА ЧЕРЕЗ ВАШУ ПОЛИТИКУ ===
            _policy.CheckAccessRights(currentUserId: currentUserId,
                                      isCurrentUserAdmin: isAdmin,
                                      targetBookingOwnerId: booking.UserId);

            try
            {
                // Если бронь была подтверждена, возвращаем место событию
                //if (booking.Status == BookingStatus.Confirmed && booking.Event != null)
                //{
                    booking.Event.ReleaseSeats(1);
                    await _eventRepository.UpdateAsync(booking.Event.Id,booking.Event);
                //}

                // Вызываем доменную логику смены статуса
                booking.Cancel();

                // Сохраняем изменения в БД
                await _bookingRepository.CancelBookingAsync(bookingId);
            }
            catch (InvalidOperationException ex)
            {
                // Пробрасываем ошибку выше для обработки глобальным фильтром ошибок
                throw new InvalidOperationException($"Невозможно отменить бронь: {ex.Message}");
            }
        }
    }
}

