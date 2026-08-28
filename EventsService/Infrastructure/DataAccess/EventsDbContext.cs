using Microsoft.EntityFrameworkCore;
using EventsService.Domain;

namespace EventsService.Infrastructure.DataAccess;

public sealed class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options) { }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<ProcessedBooking> ProcessedBookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}
