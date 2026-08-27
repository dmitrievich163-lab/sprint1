using System.Security.Claims;
using BookingsService.Application.Services;
using BookingsService.Domain;
using Microsoft.AspNetCore.Http;

namespace BookingsService.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var idString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idString))
            throw new UnauthorizedAccessException("Не удалось определить пользователя.");

        return Guid.Parse(idString);
    }

    public bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole(UserRole.Admin.ToString()) ?? false;
    }
}
