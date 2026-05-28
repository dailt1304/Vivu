using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripLocationAlternativeConfiguration : IEntityTypeConfiguration<TripLocationAlternative>
{
    public void Configure(EntityTypeBuilder<TripLocationAlternative> builder)
    {
        builder.ToTable("TripLocationAlternatives");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.TripLocation)
            .WithMany(tl => tl.Alternatives)
            .HasForeignKey(a => a.TripLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Location)
            .WithMany()
            .HasForeignKey(a => a.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TripLocationId, a.LocationId })
            .IsUnique();

        builder.Property(a => a.Priority)
            .HasDefaultValue(1);

        builder.Property(a => a.Reason)
            .HasMaxLength(500);

        builder.HasCheckConstraint(
            "CK_TripLocationAlternative_Priority",
            "\"Priority\" BETWEEN 1 AND 2");
    }
}
