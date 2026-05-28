using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogComment : Entity<Guid>
{
    public Guid BlogId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string? Content { get; set; }
    public int LikeCount { get; set; }
    public bool IsHidden { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual BlogComment? ParentComment { get; set; }
    public virtual ICollection<BlogComment> Replies { get; set; } = new List<BlogComment>();
    public virtual ICollection<BlogCommentLike> BlogCommentLikes { get; set; } = new List<BlogCommentLike>();

    private BlogComment() { }

    public static BlogComment Create(Guid blogId, Guid userId, string content)
    {
        return new BlogComment
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            UserId = userId,
            Content = content,
            LikeCount = 0,
            IsHidden = false,
            CreatedDate = DateTime.UtcNow
        };
    }
}
