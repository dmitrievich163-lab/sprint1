using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        // Конструктор принимает интерфейс репозитория, а не DbContext
        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        // Все методы просто перенаправляют вызов в репозиторий
        public async Task<Guid> CreateBookingAsync(Guid eventId)
        {
            return await _bookingRepository.CreateBookingAsync(eventId);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _bookingRepository.GetByIdAsync(bookingId);
        }

        public async Task ProcessPendingBookingAsync(Guid bookingId)
        {
            await _bookingRepository.ProcessPendingBookingAsync(bookingId);
        }

        public async Task RejectBookingAsync(Guid bookingId)
        {
            await _bookingRepository.RejectBookingAsync(bookingId);
        }

        public async Task ConfirmBookingAsync(Guid bookingId)
        {
            await _bookingRepository.ConfirmBookingAsync(bookingId);
        }
    }
}

