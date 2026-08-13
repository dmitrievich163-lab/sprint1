using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.DataAccess.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Имя таблицы
            builder.ToTable("Users");

            // Первичный ключ
            builder.HasKey(u => u.Id);

            // Настройка свойства Login (уникальный индекс + ограничения)
            builder.Property(u => u.Login)
                .IsRequired()
                .HasMaxLength(256); // Ограничение длины обязательно для индекса

            // Уникальный индекс на поле логина
            builder.HasIndex(u => u.Login)
                .IsUnique();

            // Настройка связи "Один ко многим" (Пользователь -> Бронирования)
            builder.HasMany(u => u.Bookings)          // У пользователя много броней
                .WithOne(b => b.User)                 // У каждой брони один владелец
                .HasForeignKey(b => b.UserId)         // Внешний ключ в таблице Booking
                .IsRequired()                         // Поле UserId должно быть NOT NULL
                .OnDelete(DeleteBehavior.Cascade);    // При удалении пользователя удалить его брони

            // Если у вас есть навигационное свойство Bookings внутри User,
            // EF Core может потребовать явно указать FieldName для инкапсулированной коллекции.
            // Обычно это требуется, если коллекция скрыта за ReadOnlyCollection.
            builder.Metadata.FindNavigation(nameof(User.Bookings))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
