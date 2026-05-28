using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.ToTable("Blogs");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(b => b.ShortDescription)
            .HasMaxLength(500);

        builder.Property(b => b.TotalCost)
            .HasColumnType("decimal(15,2)");

        builder.Property(b => b.ViewCount)
            .HasDefaultValue(0);

        builder.Property(b => b.SaveCount)
            .HasDefaultValue(0);

        builder.Property(b => b.CommentCount)
            .HasDefaultValue(0);

        builder.Property(b => b.LikeCount)
            .HasDefaultValue(0);

        builder.Property(b => b.ShareCount)
            .HasDefaultValue(0);

        builder.Property(b => b.RatingAverage)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(1.0m);

        builder.Property(b => b.RatingCount)
            .HasDefaultValue(0);

        builder.Property(b => b.Status)
            .HasMaxLength(20)
            .HasDefaultValue("draft");

        builder.Property(b => b.CreatedDate)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("now()");

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Relationships
        builder.HasOne(b => b.User)
            .WithMany(u => u.Blogs)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Trip)
            .WithMany(t => t.Blogs)
            .HasForeignKey(b => b.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        // Full-text search configuration
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                @"setweight(to_tsvector('simple', f_unaccent(coalesce(""Title"", ''))), 'A') ||
                  setweight(to_tsvector('simple', f_unaccent(coalesce(""ShortDescription"", ''))), 'B')",
                stored: true);

        builder.HasIndex("SearchVector")
            .HasDatabaseName("idx_blog_search_vector")
            .HasMethod("gin");
    }
}

