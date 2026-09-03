using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventsService.Domain;

namespace EventsService.Infrastructure.DataAccess.Configurations;

internal sealed class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");
        builder.HasKey(p => p.BookingId);

        builder.Property(p => p.ProcessedAt)
            .IsRequired();
    }
}
