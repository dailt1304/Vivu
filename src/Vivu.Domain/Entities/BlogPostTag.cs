namespace Vivu.Domain.Entities;

public class BlogPostTag
{
    public Guid BlogId { get; set; }
    public Guid TagId { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
    public virtual BlogTag Tag { get; set; } = null!;
}
