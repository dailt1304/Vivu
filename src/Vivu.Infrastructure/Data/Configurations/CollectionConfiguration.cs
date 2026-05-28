using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations
{
public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("Collections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasColumnType("text");

        builder.Property(c => c.CoverImageUrl)
                .IsRequired(false)
            .HasMaxLength(500);

            // A user cannot have two collections with the same name, unless they are deleted
            builder.HasIndex(c => new { c.UserId, c.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = FALSE");
        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.Collections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
}
