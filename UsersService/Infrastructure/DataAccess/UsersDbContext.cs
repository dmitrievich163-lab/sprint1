using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UsersService.Domain;

namespace UsersService.Infrastructure.DataAccess;

public sealed class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

        var passwordConverter = new ValueConverter<PasswordHash, string>(
            v => v.Value,
            s => new PasswordHash(s)
        );

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.Property(u => u.PasswordHash)
                .HasConversion(passwordConverter);
        });
    }
}
