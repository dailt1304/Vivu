namespace Vivu.Domain.Entities;

public class BlogCommentLike
{
    public Guid UserId { get; set; }
    public Guid CommentId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual BlogComment Comment { get; set; } = null!;
}
