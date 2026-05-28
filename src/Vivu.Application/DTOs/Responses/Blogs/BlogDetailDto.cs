using Vivu.Application.DTOs.Responses.Trips;

namespace Vivu.Application.DTOs.Responses.Blogs
{
    public class BlogTagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
    }

    public class BlogDetailDto
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
        public string Status { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Stats
        public int LikeCount { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int SaveCount { get; set; }
        public decimal RatingAverage { get; set; }
        public int RatingCount { get; set; }

        // Current user interaction state (null if not authenticated)
        public bool? IsLikedByCurrentUser { get; set; }
        public bool? IsBookmarkedByCurrentUser { get; set; }

        // Author info
        public string? AuthorName { get; set; }
        public string? AuthorAvatarUrl { get; set; }

        // Content
        public List<BlogStoryDayDto> BlogStoryDays { get; set; } = new();
        public List<BlogTagDto> Tags { get; set; } = new();

        // Trip locations (flattened from all TripDays of the linked Trip)
        public List<TripLocationDto> TripLocations { get; set; } = new();
    }
}
