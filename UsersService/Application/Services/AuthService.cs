using System.ComponentModel.DataAnnotations;
using UsersService.Application.Models;
using UsersService.Application.Repositories;
using UsersService.Application.Services;
using UsersService.Domain;

namespace UsersService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByLoginAsync(request.Login);
        if (existingUser != null)
            throw new ValidationException("Пользователь с таким логином уже существует.");

        var role = request.Role ?? UserRole.User;
        var user = User.Create(request.Login, PasswordHash.CreateFromPlainText(request.Password), role);

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user.Id, user.Login, user.Role);
        return new AuthResponse { Token = token };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByLoginAsync(request.Login);

        if (user == null || !user.PasswordHash.Verify(request.Password))
        {
            throw new UnauthorizedAccessException("Неверный логин или пароль.");
        }

        var token = _jwtTokenService.GenerateToken(user.Id, user.Login, user.Role);
        return new AuthResponse { Token = token };
    }
}
