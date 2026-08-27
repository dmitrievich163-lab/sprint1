using System.Security.Cryptography;
using System.Text;

namespace UsersService.Domain;

public sealed record PasswordHash(string Value)
{
    public static PasswordHash CreateFromPlainText(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return new PasswordHash(Convert.ToHexString(bytes));
    }

    public bool Verify(string password)
    {
        var inputBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = Convert.FromHexString(Value);

        var computedHash = SHA256.HashData(inputBytes);

        return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
    }
}
