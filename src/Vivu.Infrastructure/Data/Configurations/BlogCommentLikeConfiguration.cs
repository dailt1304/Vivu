using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class BlogCommentLikeConfiguration : IEntityTypeConfiguration<BlogCommentLike>
{
    public void Configure(EntityTypeBuilder<BlogCommentLike> builder)
    {
        builder.ToTable("BlogCommentLikes");

        // Composite primary key
        builder.HasKey(bcl => new { bcl.UserId, bcl.CommentId });

        builder.Property(bcl => bcl.CreatedAt)
            .HasDefaultValueSql("now()");

        // Relationships
        builder.HasOne(bcl => bcl.User)
            .WithMany()
            .HasForeignKey(bcl => bcl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bcl => bcl.Comment)
            .WithMany(c => c.BlogCommentLikes)
            .HasForeignKey(bcl => bcl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
