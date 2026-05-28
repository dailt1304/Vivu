using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Blog : Entity<Guid>
{
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
    
    // Counters
    public int ViewCount { get; set; }
    public int SaveCount { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public int ShareCount { get; set; }
    public decimal RatingAverage { get; set; } = 1.0m;
    public int RatingCount { get; set; }
    
    public string Status { get; set; } = "draft";
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Trip? Trip { get; set; }
    public virtual ICollection<BlogStoryDay> BlogStoryDays { get; set; } = new List<BlogStoryDay>();
    public virtual ICollection<BlogImage> BlogImages { get; set; } = new List<BlogImage>();
    public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    public virtual ICollection<BlogSave> BlogSaves { get; set; } = new List<BlogSave>();
    public virtual ICollection<BlogView> BlogViews { get; set; } = new List<BlogView>();
    public virtual ICollection<BlogComment> BlogComments { get; set; } = new List<BlogComment>();
    public virtual ICollection<BlogReport> BlogReports { get; set; } = new List<BlogReport>();
    public virtual ICollection<BlogLike> BlogLikes { get; set; } = new List<BlogLike>();

    public static Blog Create(
        Guid userId,
        Guid? tripId,
        string title,
        string slug,
        string? coverImageUrl,
        string? shortDescription,
        DateTime? travelDateStart,
        DateTime? travelDateEnd,
        decimal? totalCost,
        int? groupSize)
    {
        return new Blog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TripId = tripId,
            Title = title,
            Slug = slug,
            CoverImageUrl = coverImageUrl,
            ShortDescription = shortDescription,
            TravelDateStart = travelDateStart,
            TravelDateEnd = travelDateEnd,
            TotalCost = totalCost,
            GroupSize = groupSize,
            Status = "draft",
            CreatedDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public BlogLike AddLike(Guid userId)
    {
        var like = BlogLike.Create(Id, userId);
        BlogLikes.Add(like);
        LikeCount++;
        UpdatedAt = DateTime.UtcNow;
        return like;
    }

    public void RemoveLike(BlogLike like)
    {
        BlogLikes.Remove(like);
        LikeCount = Math.Max(0, LikeCount - 1);
        UpdatedAt = DateTime.UtcNow;
    }
}
