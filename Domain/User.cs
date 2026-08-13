using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Login { get; private set; }
        public PasswordHash PasswordHash { get; private set; } // Value Object для инкапсуляции хеша
        public UserRole Role { get; private set; }

        // Навигационное свойство для связей (инкапсулировано)
        private readonly List<Booking> _bookings = new();
        public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

        private User()
        {
        }

        // Фабричный метод
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
        public Booking CreateBooking(Guid eventId)
        {
            var booking = new Booking(eventId, this.Id);
            _bookings.Add(booking); // Связываем со стороны пользователя
            return booking;
        }

    }
}
