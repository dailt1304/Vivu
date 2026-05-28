using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogLike : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid BlogId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Blog Blog { get; set; } = null!;

    private BlogLike() { }

    public static BlogLike Create(Guid blogId, Guid userId)
    {
        return new BlogLike
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            UserId = userId,
            CreatedDate = DateTime.UtcNow
        };
    }
}
