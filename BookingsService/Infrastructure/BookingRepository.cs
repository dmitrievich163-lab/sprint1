using Microsoft.EntityFrameworkCore;
using BookingsService.Application.Repositories;
using BookingsService.Domain;
using BookingsService.Infrastructure.DataAccess;

namespace BookingsService.Infrastructure;

public class BookingRepository : IBookingRepository
{
    private readonly BookingsDbContext _context;

    public BookingRepository(BookingsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateBookingAsync(Guid eventId, Guid userId)
    {
        var booking = new Booking(eventId, userId);
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
        return booking.Id;
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<IReadOnlyCollection<Booking>> GetActiveBookingsByUserIdAsync(Guid userId)
    {
        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync();

        return bookings.AsReadOnly();
    }

    public async Task ProcessPendingBookingAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task RejectBookingAsync(Guid bookingId)
    {
        await _context.SaveChangesAsync();
    }

    public async Task ConfirmBookingAsync(Guid bookingId)
    {
        await _context.SaveChangesAsync();
    }

    public async Task CancelBookingAsync(Guid bookingId)
    {
        await _context.SaveChangesAsync();
    }
}
