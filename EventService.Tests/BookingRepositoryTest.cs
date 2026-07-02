using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Exceptions;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using AspNetCoreApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventService.Tests
{
    public class BookingRepositoryTest : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

        private string? _connectionString;
        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
            await using var setupContext = CreateContextForMigration();

            await setupContext.Database.MigrateAsync();
        }

        private AppDbContext CreateContextForMigration()
        {
            if (_connectionString == null)
                throw new InvalidOperationException("Connection string is not initialized.");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                 .UseNpgsql(_connectionString)
                 .Options;

            return new AppDbContext(options);
        }


        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Bookings\", \"Events\" RESTART IDENTITY CASCADE");
        }
        [Fact]
        public async Task CreateBookingAsync_CreatesBookingAndDecreasesEventSeats()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            // Создаем событие с 10 местами
            var @event = new Event { Title = "Тестовый концерт", TotalSeats = 10, AvailableSeats = 10 };
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context); // Предполагаемое имя сервиса

            // Act
            var newBookingId = await bookingRepository.CreateBookingAsync(@event.Id);

            // Assert: Проверяем, что ID сгенерирован
            Assert.NotEqual(Guid.Empty, newBookingId);

            // Assert: Проверяем состояние в БД через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(newBookingId);
            var eventFromDb = await verificationContext.Events.FindAsync(@event.Id);

            Assert.NotNull(bookingFromDb);
            Assert.Equal(@event.Id, bookingFromDb.EventId);
            Assert.Equal(BookingStatus.Pending, bookingFromDb.Status);

            Assert.NotNull(eventFromDb);
            Assert.Equal(9, eventFromDb.AvailableSeats); // Проверяем, что место зарезервировано
        }

        [Fact]
        public async Task ProcessPendingBookingAsync_ConfirmsWhenSeatsAvailable()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Полный зал", TotalSeats = 1, AvailableSeats = 1 };
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            // Создаем бронь в статусе Pending
            var pendingBooking = new Booking(@event.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            await bookingRepository.ProcessPendingBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);
            var eventFromDb = await verificationContext.Events.FindAsync(@event.Id);

            Assert.Equal(BookingStatus.Confirmed, bookingFromDb.Status);
            Assert.Equal(0, eventFromDb.AvailableSeats); // Место окончательно занято
        }

        [Fact]
        public async Task ProcessPendingBookingAsync_RejectsWhenNoSeatsAvailable()
        {
            // Arrange: Создаем событие без мест
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Пустой зал", TotalSeats = 0, AvailableSeats = 0 };
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var pendingBooking = new Booking(@event.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            await bookingRepository.ProcessPendingBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);

            Assert.Equal(BookingStatus.Rejected, bookingFromDb.Status);
        }

        [Fact]
        public async Task RejectBookingAsync_RejectsPendingBooking()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Концерт", TotalSeats = 10, AvailableSeats = 10 };
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var pendingBooking = new Booking(@event.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            await bookingRepository.RejectBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);

            Assert.Equal(BookingStatus.Rejected, bookingFromDb.Status);
        }

        [Fact]
        public async Task ConfirmBookingAsync_ConfirmsPendingBooking()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();

            var @event = new Event { Title = "Концерт", TotalSeats = 10, AvailableSeats = 10 };
            await context.Events.AddAsync(@event);
            await context.SaveChangesAsync();

            var pendingBooking = new Booking(@event.Id) { Status = BookingStatus.Pending };
            await context.Bookings.AddAsync(pendingBooking);
            await context.SaveChangesAsync();

            var bookingRepository = new BookingRepository(context);

            // Act
            await bookingRepository.ConfirmBookingAsync(pendingBooking.Id);

            // Assert: Проверяем через новый контекст
            await using var verificationContext = CreateContext();
            var bookingFromDb = await verificationContext.Bookings.FindAsync(pendingBooking.Id);

            Assert.Equal(BookingStatus.Confirmed, bookingFromDb.Status);
        }
    }
}
