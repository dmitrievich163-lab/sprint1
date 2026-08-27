namespace UsersService.Application.Models;

public sealed class RegisterRequest
{
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UsersService.Domain.UserRole? Role { get; set; }
}
