using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, string login, UserRole role);
    }
}
