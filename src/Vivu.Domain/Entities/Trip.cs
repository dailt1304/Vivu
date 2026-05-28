using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Trip : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid? CityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? InviteCode { get; set; }
    public int? TripSize { get; set; }
    public string Status { get; set; } = "planning";
    public bool IsPublic { get; set; }
    public string? PersonalizationContextJson { get; set; }
    public string? ConstraintsJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual City? City { get; set; }
    public virtual ICollection<TripMember> TripMembers { get; set; } = new List<TripMember>();
    public virtual ICollection<TripDay> TripDays { get; set; } = new List<TripDay>();
    public virtual TripRating? TripRating { get; set; }
    public virtual ICollection<TripFavorite> TripFavorites { get; set; } = new List<TripFavorite>();
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    public void Update(
        string title,
        string? description,
        DateTime? startDate,
        DateTime? endDate,
        string? coverUrl,
        int? tripSize,
        string status)
    {
        Title = title;
        Description = description ?? Description;
        StartDate = startDate ?? StartDate;
        EndDate = endDate ?? EndDate;
        CoverUrl = coverUrl ?? CoverUrl;
        TripSize = tripSize ?? TripSize;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public bool CanBeModifiedBy(Guid userId, ICollection<TripMember> members)
    {
        if (UserId == userId)
            return true;
        
        return members.Any(m => m.UserId == userId && (m.Role == "editor" || m.Role == "organizer"));
    }

    public bool IsValidDateRange()
    {
        if (StartDate.HasValue && EndDate.HasValue)
        {
            return StartDate.Value < EndDate.Value;
        }
        return true; 
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public bool CanBeDeletedBy(Guid userId)
    {
        // Only owner can delete
        return UserId == userId;
    }
    
    private Trip() { }

    public static Trip Create(
        Guid userId,
        string title,
        string? description = null,
        string? coverUrl = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? tripSize = null,
        bool isPublic = false,
        string? inviteCode = null,
        Guid? cityId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        return new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CityId = cityId,
            Title = title,
            Description = description,
            CoverUrl = coverUrl,
            StartDate = startDate,
            EndDate = endDate,
            TripSize = tripSize,
            IsPublic = isPublic,
            InviteCode = inviteCode,
            Status = "planning",
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    public void UpdateStatus(string newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public bool ShouldBeOngoing()
    {
        return Status == "planning" 
            && StartDate.HasValue 
            && DateTime.UtcNow >= StartDate.Value;
    }

    public bool ShouldBeCompleted()
    {
        return Status == "ongoing" 
            && EndDate.HasValue 
            && DateTime.UtcNow >= EndDate.Value;
    }

    public bool IsCompleteForSharing()
    {
        return !string.IsNullOrWhiteSpace(Title)
            && StartDate.HasValue
            && EndDate.HasValue
            && IsValidDateRange()
            && !string.IsNullOrWhiteSpace(Description);
    }

    public void MakePublic()
    {
        IsPublic = true;
        UpdatedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }

    public void MakePrivate()
    {
        IsPublic = false;
        UpdatedAt = DateTime.UtcNow;
        ModifiedDate = DateTime.UtcNow;
    }
}
