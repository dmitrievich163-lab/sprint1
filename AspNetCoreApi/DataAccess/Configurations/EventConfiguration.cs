using AspNetCoreApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreApi.DataAccess.Configurations
{
    internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            // Указываем имя таблицы в базе данных
            builder.ToTable("Events");

            // Настройка первичного ключа
            builder.HasKey(e => e.Id);

            // Указываем, что значение Id генерируется в коде (Guid.NewGuid()), а не в БД
            builder.Property(e => e.Id).ValueGeneratedNever();

            // Настройка ограничений для свойств
            builder.Property(e => e.Title)
                .IsRequired()             // Поле обязательно
                .HasMaxLength(250);       // Максимальная длина строки

            builder.Property(e => e.Description)
                .HasMaxLength(1000);      // Ограничение длины для описания

            builder.Property(e => e.StartAt)
                .IsRequired();           // Время начала обязательно

            builder.Property(e => e.EndAt)
                .IsRequired();           // Время окончания обязательно

            builder.Property(e => e.TotalSeats)
                .IsRequired();           // Время окончания обязательно

            // --- НАСТРОЙКА СВЯЗИ "ОДИН-КО-МНОГИМ" ---
            // Один Event может иметь много Bookings

            // Определяем навигационное свойство коллекции
            builder.HasMany(e => e.Bookings)
                   // Указываем обратное навигационное свойство в Booking
                   .WithOne(b => b.Event)
                   // Задаем внешний ключ в зависимой сущности (Booking)
                   .HasForeignKey(b => b.EventId)
                   // Устанавливаем правило каскадного удаления:
                   // при удалении Event будут удалены все связанные с ним Bookings
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

