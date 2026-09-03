using BookingsService.Application.Repositories;
using BookingsService.Application.Services;
using BookingsService.Domain;
using SharedContracts;

namespace BookingsService.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingPolicy _policy;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUser;

    public BookingService(
        IBookingRepository bookingRepository,
        IBookingPolicy policy,
        IEventPublisher eventPublisher,
        ICurrentUserService currentUser)
    {
        _bookingRepository = bookingRepository;
        _policy = policy;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
    }

    public async Task<Guid> CreateBookingAsync(Guid eventId, Guid userId)
    {
        var activeBookings = await _bookingRepository.GetActiveBookingsByUserIdAsync(userId);
        _policy.CheckActiveBookingLimit(activeBookings, 10);

        return await _bookingRepository.CreateBookingAsync(eventId, userId);
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        return await _bookingRepository.GetByIdAsync(bookingId);
    }

    public async Task ProcessPendingBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null || booking.Status != BookingStatus.Pending)
            return;

        booking.Confirm();
        await _bookingRepository.ConfirmBookingAsync(bookingId);

        await _eventPublisher.PublishBookingConfirmedAsync(new BookingConfirmed
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            SeatCount = 1,
            ConfirmedAt = booking.ProcessedAt!.Value
        });
    }

    public async Task RejectBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            return;

        booking.Reject();
        await _bookingRepository.RejectBookingAsync(bookingId);
    }

    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Бронь с ID {bookingId} не найдена.");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Невозможно подтвердить бронь со статусом {booking.Status}.");

        booking.Confirm();
        await _bookingRepository.ConfirmBookingAsync(bookingId);

        await _eventPublisher.PublishBookingConfirmedAsync(new BookingConfirmed
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            SeatCount = 1,
            ConfirmedAt = booking.ProcessedAt!.Value
        });
    }

    public async Task CancelBookingAsync(Guid bookingId)
    {
        var currentUserId = _currentUser.GetCurrentUserId();
        bool isAdmin = _currentUser.IsAdmin();

        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Бронирование {bookingId} не найдено.");

        _policy.CheckAccessRights(
            currentUserId: currentUserId,
            isCurrentUserAdmin: isAdmin,
            targetBookingOwnerId: booking.UserId);

        try
        {
            booking.Cancel();
            await _bookingRepository.CancelBookingAsync(bookingId);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Невозможно отменить бронь: {ex.Message}");
        }
    }
}
