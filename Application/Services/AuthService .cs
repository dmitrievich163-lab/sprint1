using Application.Repositories;
using Domain;
using Presentation.API.Models.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.Services
{
    public class AuthService:IAuthService
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
            // Проверяем занятость логина
            var existingUser = await _userRepository.GetByLoginAsync(request.Login);
            if (existingUser != null)
                throw new ValidationException("Пользователь с таким логином уже существует.");

            // Создаем доменную сущность (здесь отработает хеширование пароля)
            var role = request.Role ?? UserRole.User;
            var user = User.Create(request.Login, PasswordHash.CreateFromPlainText(request.Password), role);

            _userRepository.Add(user);
            await _userRepository.SaveChangesAsync();

            // Генерируем токен для нового юзера
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
}
