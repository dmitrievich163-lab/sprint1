using Domain;

namespace Presentation.API.Models.Auth
{
    public class RegisterRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public UserRole? Role { get; set; } // Для тестов разрешаем Admin
    }
}
