using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogView : Entity<Guid>
{
    public Guid BlogId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? ReferrerUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime ViewedAt { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
    public virtual User? User { get; set; }
}
