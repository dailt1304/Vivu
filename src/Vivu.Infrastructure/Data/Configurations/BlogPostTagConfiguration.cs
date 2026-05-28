using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data.Configurations;

public class BlogPostTagConfiguration : IEntityTypeConfiguration<BlogPostTag>
{
    public void Configure(EntityTypeBuilder<BlogPostTag> builder)
    {
        builder.ToTable("BlogPostTags");

        // Composite primary key
        builder.HasKey(bpt => new { bpt.BlogId, bpt.TagId });

        // Relationships
        builder.HasOne(bpt => bpt.Blog)
            .WithMany(b => b.BlogPostTags)
            .HasForeignKey(bpt => bpt.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bpt => bpt.Tag)
            .WithMany(t => t.BlogPostTags)
            .HasForeignKey(bpt => bpt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
