using Presentation.API.Models.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IAuthService
    {
        Task<Presentation.API.Models.Auth.AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
