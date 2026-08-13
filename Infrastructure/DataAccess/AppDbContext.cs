using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.DataAccess
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            var passwordConverter = new ValueConverter<PasswordHash, string>(
         v => v.Value,
         s => new PasswordHash(s)
     );

            modelBuilder.Entity<User>(user =>
            {
                user.ToTable("Users");

                // Применяем конвертер к свойству
                user.Property(u => u.PasswordHash)
                    .HasConversion(passwordConverter); // <--- Это должно остаться!

                // ... ваши настройки Login ...
            });

        }

     
    }
}
