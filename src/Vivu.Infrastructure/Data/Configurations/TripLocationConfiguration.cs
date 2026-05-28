using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripLocationConfiguration : IEntityTypeConfiguration<TripLocation>
{
    public void Configure(EntityTypeBuilder<TripLocation> builder)
    {
        builder.ToTable("TripLocations");

        builder.HasKey(tl => tl.Id);

        builder.HasOne(tl => tl.TripDay)
            .WithMany(td => td.TripLocations)
            .HasForeignKey(tl => tl.TripDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tl => tl.Location)
            .WithMany(l => l.TripLocations)
            .HasForeignKey(tl => tl.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}