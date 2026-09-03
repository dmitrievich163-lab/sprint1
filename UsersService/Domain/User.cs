namespace UsersService.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    private User() { }

    public static User Create(string login, PasswordHash passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new Exception("Логин не может быть пустым.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
