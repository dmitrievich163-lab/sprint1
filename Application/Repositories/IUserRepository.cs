using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid userId);

        Task<IReadOnlyCollection<Booking>> GetActiveBookingsByUserIdAsync(Guid userId);

        Task<User?> GetByLoginAsync(string login);

        void Add(User user);

        /// <summary>
        /// Асинхронно сохраняет все изменения в базе данных.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
