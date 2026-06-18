using AspNetCoreApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCoreApi.DataAccess.Configurations
{
    internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            // Указываем имя таблицы в базе данных
            builder.ToTable("Bookings");

            // Настройка первичного ключа
            builder.HasKey(b => b.Id);

            // Значение Id генерируется в коде конструктора
            builder.Property(b => b.Id).ValueGeneratedNever();

            // --- ХРАНЕНИЕ ENUM КАК СТРОКИ В БД ---
            // По умолчанию EF Core сохраняет enum как число (int).
            // Эта настройка заставляет его сохранять статус как строку (например, "Confirmed").
            builder.Property(b => b.Status)
                .HasConversion(
                    v => v.ToString(), // Преобразуем из enum в string при сохранении
                    v => (BookingStatus)Enum.Parse(typeof(BookingStatus), v) // И обратно при чтении
                );

            // Свойство CreatedAt обычно устанавливается один раз при создании
            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .ValueGeneratedNever(); // Значение задается в конструкторе

            // Свойство ProcessedAt может быть null
            builder.Property(b => b.ProcessedAt)
                .IsRequired(false);

            // --- НАСТРОЙКА СВЯЗИ С EVENT ---
            // Внешний ключ уже определен в модели, здесь мы можем уточнить конфигурацию

            // Навигационное свойство-ссылка на родительский Event
            builder.HasOne(b => b.Event)
                   // Обратная сторона связи - коллекция Bookings в Event
                   .WithMany(e => e.Bookings)
                   // Явно указываем имя столбца внешнего ключа
                   .HasForeignKey(b => b.EventId)
                   // Запрещаем удаление Event, если у него есть связанные Bookings
                   // Это предотвращает случайную потерю данных о бронированиях
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
