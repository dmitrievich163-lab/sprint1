using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain
{
    public sealed record PasswordHash(string Value)
    {
        

        //internal PasswordHash(string Value)
        //{
        //    this.Value = Value;
        //}

        /// <summary>
        /// Создает хеш из открытого текста пароля
        /// </summary>
        public static PasswordHash CreateFromPlainText(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
            }

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return new PasswordHash(Convert.ToHexString(bytes));
        }

        /// <summary>
        /// Проверяет соответствие пароля сохраненному хешу
        /// </summary>
        public bool Verify(string password)
        {
            // Для защиты от атак по времени используем CryptographicOperations.FixedTimeEquals
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = Convert.FromHexString(Value);

            var computedHash = SHA256.HashData(inputBytes);

            return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
        }
    }
}

