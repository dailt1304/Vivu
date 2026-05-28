using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations
{
public class CollectionLocationConfiguration : IEntityTypeConfiguration<CollectionLocation>
{
    public void Configure(EntityTypeBuilder<CollectionLocation> builder)
    {
        builder.ToTable("CollectionLocations");

        // Composite primary key
        builder.HasKey(cl => new { cl.CollectionId, cl.LocationId });

        builder.Property(cl => cl.Note)
            .HasColumnType("text");

        builder.Property(cl => cl.AddedAt)
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne(cl => cl.Collection)
            .WithMany(c => c.CollectionLocations)
            .HasForeignKey(cl => cl.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cl => cl.Location)
            .WithMany(l => l.CollectionLocations)
            .HasForeignKey(cl => cl.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
}
