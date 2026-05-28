using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class BlogReport : Entity<Guid>
{
    public Guid BlogId { get; set; }
    public Guid ReporterId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string ReportReason { get; set; } = string.Empty;
    public string? ReportDescription { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? AdminNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Blog Blog { get; set; } = null!;
    public virtual User Reporter { get; set; } = null!;
    public virtual User? Reviewer { get; set; }

    public static BlogReport Create(
        Guid blogId,
        Guid reporterId,
        string reportType,
        string reportReason,
        string? reportDescription = null)
    {
        return new BlogReport
        {
            Id = Guid.NewGuid(),
            BlogId = blogId,
            ReporterId = reporterId,
            ReportType = reportType,
            ReportReason = reportReason,
            ReportDescription = reportDescription,
            Status = "PENDING",
            CreatedDate = DateTime.UtcNow
        };
    }

    public void UpdateStatus(string status, string? adminNote = null)
    {
        Status = status;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }
}
