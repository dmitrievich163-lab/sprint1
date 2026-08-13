using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;
using Domain;
using Application.Repositories;

namespace Infrastructure
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateBookingAsync(Guid eventId, Guid userId)
        {
            var booking = new Booking(eventId,userId);
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            return booking.Id;
        }

        public async Task<Booking?> GetByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId);
        }

       
        public async Task ProcessPendingBookingAsync()
        {
                await _context.SaveChangesAsync();
        }

        public async Task RejectBookingAsync(Guid bookingId)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Booking?> GetByIdWithEventAsync(Guid bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        public async Task ConfirmBookingAsync(Guid bookingId)
        {
            await _context.SaveChangesAsync();
        }
    }
}
