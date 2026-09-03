using UsersService.Domain;

namespace UsersService.Application.Services;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string login, UserRole role);
}
