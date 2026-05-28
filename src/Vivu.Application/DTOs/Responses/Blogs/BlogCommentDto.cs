namespace Vivu.Application.DTOs.Responses.Blogs
{
    public class BlogCommentDto
    {
        public Guid Id { get; set; }
        public Guid BlogId { get; set; }
        public Guid UserId { get; set; }
        public string? Content { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Author info
        public string? AuthorName { get; set; }
        public string? AuthorAvatarUrl { get; set; }
    }
}
