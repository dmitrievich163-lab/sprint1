namespace Presentation.API.Models.Auth
{
    public class LoginRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
