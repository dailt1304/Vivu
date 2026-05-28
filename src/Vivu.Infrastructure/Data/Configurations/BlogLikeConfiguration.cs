using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class BlogLikeConfiguration : IEntityTypeConfiguration<BlogLike>
{
    public void Configure(EntityTypeBuilder<BlogLike> builder)
    {
        builder.ToTable("BlogLikes");

        builder.HasKey(bl => bl.Id);

        // Unique constraint: mỗi user chỉ like 1 blog 1 lần
        builder.HasIndex(bl => new { bl.BlogId, bl.UserId })
            .IsUnique();

        builder.Property(bl => bl.CreatedDate)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne(bl => bl.User)
            .WithMany()
            .HasForeignKey(bl => bl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bl => bl.Blog)
            .WithMany(b => b.BlogLikes)
            .HasForeignKey(bl => bl.BlogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
