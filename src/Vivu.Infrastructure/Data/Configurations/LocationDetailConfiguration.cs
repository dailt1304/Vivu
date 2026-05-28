using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class LocationDetailConfiguration : IEntityTypeConfiguration<LocationDetail>
{
    public void Configure(EntityTypeBuilder<LocationDetail> builder)
    {
        builder.ToTable("LocationDetails");

        builder.HasKey(ld => ld.LocationId);

        builder.Property(ld => ld.Phone)
            .HasMaxLength(40);

        builder.Property(ld => ld.Website)
            .HasColumnType("text");

        builder.Property(ld => ld.OpeningHours)
            .HasColumnType("text");

        builder.Property(ld => ld.Tags)
            .HasColumnType("text");

        // PostgreSQL jsonb column for images
        builder.Property(ld => ld.Images)
            .HasColumnType("jsonb");

        // Relationship
        builder.HasOne(ld => ld.Location)
            .WithOne(l => l.LocationDetail)
            .HasForeignKey<LocationDetail>(ld => ld.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
