namespace Vivu.Application.DTOs.Responses.Blogs
{
    public class PublicBlogDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? TripId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string? ShortDescription { get; set; }
        public DateTime? TravelDateStart { get; set; }
        public DateTime? TravelDateEnd { get; set; }
        public decimal? TotalCost { get; set; }
        public int? GroupSize { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string Status { get; set; } = string.Empty;

        // Basic stats
        public int LikeCount { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int SaveCount { get; set; }

        // Author info
        public string? AuthorName { get; set; }
        public string? AuthorAvatarUrl { get; set; }

        // Current user interaction state (null if not authenticated)
        public bool? IsLikedByCurrentUser { get; set; }
        public bool? IsBookmarkedByCurrentUser { get; set; }
    }
}
