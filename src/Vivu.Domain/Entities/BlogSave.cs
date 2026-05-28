using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogSave : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid BlogId { get; set; }
    public string CollectionName { get; set; } = "Default";

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Blog Blog { get; set; } = null!;

    private BlogSave() { }

    public static BlogSave Create(Guid blogId, Guid userId)
    {
        return new BlogSave
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            UserId = userId,
            CollectionName = "Default",
            CreatedDate = DateTime.UtcNow
        };
    }
}
