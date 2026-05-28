using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Vivu.Domain.Entities;
using Vivu.Domain.Shared;

namespace Vivu.Infrastructure.Data.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Address)
            .HasMaxLength(255);

        builder.Property(l => l.Latitude)
            .HasColumnType("double precision");

        builder.Property(l => l.Longitude)
            .HasColumnType("double precision");

        builder.Property(l => l.RatingAverage)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0);

        builder.Property(l => l.RatingCount)
            .HasDefaultValue(0);

        builder.Property(l => l.IsVerified)
            .HasDefaultValue(true);

        builder.Property(l => l.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(e => e.LocationPoint)
          .HasColumnType("geometry(Point, 4326)");

        // Indexes
        builder.HasIndex(x => x.CityId)
              .HasDatabaseName("idx_loc_city_id");

        builder.HasIndex(x => new { x.CityId, x.RatingAverage })
              .HasDatabaseName("idx_loc_city_rating")
              .IsDescending(false, true)
              .HasFilter("\"IsVerified\" = true");

        builder.HasIndex(x => new { x.CityId, x.CategoryId })
              .HasDatabaseName("idx_loc_city_category")
              .HasFilter("\"IsVerified\" = true");

        builder.HasIndex(x => x.CityId)
              .HasDatabaseName("idx_loc_city_id");

        builder.HasIndex(x => x.LocationPoint)
              .HasDatabaseName("idx_loc_spatial")
              .HasMethod("gist");

        // Relationships
        builder.HasOne(l => l.City)
            .WithMany(c => c.Locations)
            .HasForeignKey(l => l.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Category)
            .WithMany(c => c.Locations)
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Full-text search configuration
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                @"setweight(to_tsvector('simple', f_unaccent(coalesce(""Name"", ''))), 'A') ||
                  setweight(to_tsvector('simple', f_unaccent(coalesce(""Description"", ''))), 'B') ||
                  setweight(to_tsvector('simple', f_unaccent(coalesce(""Address"", ''))), 'C')",
                stored: true);

        builder.HasIndex("SearchVector")
            .HasDatabaseName("idx_location_search_vector")
            .HasMethod("gin");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("idx_location_name_trgm")
            .HasMethod("gin")
            .HasAnnotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

        
    }
}
