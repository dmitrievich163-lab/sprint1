namespace BookingsService.Application.Services;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
    bool IsAdmin();
}
