using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class LocationReport : Entity<Guid>
{
    public Guid LocationId { get; set; }
    public Guid? UserId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string ReportReason { get; set; } = string.Empty;
    public string? ReportDescription { get; set; }
    public string? SuggestedName { get; set; }
    public string? SuggestedAddress { get; set; }
    public string? EvidenceImages { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? AdminNote { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Location Location { get; set; } = null!;
    public virtual User? User { get; set; }

    public static LocationReport Create(
        Guid locationId,
        Guid userId,
        string reportType,
        string reportReason,
        string? reportDescription = null,
        string? suggestedName = null,
        string? suggestedAddress = null,
        string? evidenceImages = null)
    {
        return new LocationReport
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            UserId = userId,
            ReportType = reportType,
            ReportReason = reportReason,
            ReportDescription = reportDescription,
            SuggestedName = suggestedName,
            SuggestedAddress = suggestedAddress,
            EvidenceImages = evidenceImages,
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

    public void UpdateContent(
        string reportType,
        string reportReason,
        string? reportDescription = null,
        string? suggestedName = null,
        string? suggestedAddress = null,
        string? evidenceImages = null)
    {
        ReportType = reportType;
        ReportReason = reportReason;
        ReportDescription = reportDescription;
        SuggestedName = suggestedName;
        SuggestedAddress = suggestedAddress;
        
        if (evidenceImages != null)
        {
            EvidenceImages = evidenceImages;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
