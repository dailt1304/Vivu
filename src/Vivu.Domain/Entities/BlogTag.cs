using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogTag : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int UseCount { get; set; }

    // Navigation Properties
    public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();

    public static BlogTag Create(string name, string? slug = null)
    {
        return new BlogTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            UseCount = 0,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }
}
