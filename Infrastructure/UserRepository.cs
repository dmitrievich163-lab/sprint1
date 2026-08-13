using Application.Repositories;
using Domain;
using Infrastructure.DataAccess;
using System;
using System.Collections.Generic;
using System.Text;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
   public class UserRepository: IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            // Базовая загрузка пользователя без броней (если они не нужны сразу)
            return await _context.Users
                .AsNoTracking() // Оптимизация для чтения
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IReadOnlyCollection<Booking>> GetActiveBookingsByUserIdAsync(Guid userId)
        {
            // Получаем только активные брони (Pending, Confirmed), исключая отмененные и завершенные
            // AsSplitQuery критически важен, чтобы EF Core не пытался сделать один гигантский JOIN,
            // который дублирует строки пользователей при наличии нескольких броней.
            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Event) // Включаем событие, если оно нужно для логики в сервисе
                .Where(b => b.UserId == userId &&
                            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .AsSplitQuery() // Разделяет запрос на несколько SQL-запросов
                .ToListAsync();

            return bookings.AsReadOnly();
        }
        public async Task<User?> GetByLoginAsync(string login)
        {
            // Используем AsNoTracking, так как мы только читаем данные для проверки пароля
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLowerInvariant());

            /* 
               Обратите внимание на ToLowerInvariant().
               Это делает поиск нечувствительным к регистру (TestUser = testuser).
               Если у вас в БД стоит COLLATE SQL_Latin1_General_CP1_CI_AS, можно обойтись без него,
               но явное приведение к нижнему регистру надежнее.
            */
        }

        public void Add(User user)
        {
            // Говорим EF Core: "Начни следить за этим новым объектом"
            _context.Users.Add(user);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            // Выполняем SQL INSERT / UPDATE / DELETE
            await _context.SaveChangesAsync(ct);
        }
    }
}
