using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogImage : Entity<Guid>
{
    public Guid BlogId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsThumbnail { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? AltText { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
}
