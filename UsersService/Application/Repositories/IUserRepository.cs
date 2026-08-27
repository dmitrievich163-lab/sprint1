using UsersService.Domain;

namespace UsersService.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByLoginAsync(string login);
    void Add(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
