using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogStoryDay : Entity<Guid>
{
    public Guid BlogId { get; set; }
    public string BlockType { get; set; } = "location";
    public int DayNumber { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? DestinationName { get; set; }
    public Guid? LocationId { get; set; }
    public string? ImageUrl { get; set; }
    public string? QuoteAuthor { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
    public static BlogStoryDay Create(
        Guid blogId,
        string blockType,
        int dayNumber,
        string? title,
        string? content,
        string? destinationName,
        Guid? locationId,
        string? imageUrl,
        int displayOrder,
        string? quoteAuthor = null)
    {
        return new BlogStoryDay
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            BlockType = blockType,
            DayNumber = dayNumber,
            Title = title,
            Content = content,
            DestinationName = destinationName,
            LocationId = locationId,
            ImageUrl = imageUrl,
            QuoteAuthor = quoteAuthor,
            DisplayOrder = displayOrder,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }
}
