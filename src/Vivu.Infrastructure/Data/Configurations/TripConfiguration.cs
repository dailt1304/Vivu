using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.InviteCode)
            .HasMaxLength(50);

        builder.HasIndex(t => t.InviteCode)
            .IsUnique();

        builder.Property(t => t.Status)
            .HasMaxLength(20)
            .HasDefaultValue("planning");

        builder.Property(t => t.IsPublic)
            .HasDefaultValue(false);

        builder.Property(t => t.PersonalizationContextJson)
            .HasColumnType("jsonb");

        builder.Property(t => t.ConstraintsJson)
            .HasColumnType("jsonb");

        builder.Property(t => t.CreatedDate)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("now()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany(u => u.Trips)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Full-text search configuration
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                @"setweight(to_tsvector('simple', f_unaccent(coalesce(""Title"", ''))), 'A') ||
                  setweight(to_tsvector('simple', f_unaccent(coalesce(""Description"", ''))), 'B')",
                stored: true);

        builder.HasIndex("SearchVector")
            .HasDatabaseName("idx_trip_search_vector")
            .HasMethod("gin");
    }
}
