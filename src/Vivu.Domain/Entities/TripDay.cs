using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class TripDay : Entity<Guid>
{
    public Guid TripId { get; set; }
    public string? Title { get; set; }
    public DateTime? DayDate { get; set; }
    public int DayIndex { get; set; }

    // Navigation Properties
    public virtual Trip Trip { get; set; } = null!;

    public virtual ICollection<TripLocation> TripLocations { get; set; } = new List<TripLocation>();

    public static TripDay Create(
        Guid TripId,
        string? Tittle = null,
        DateTime? DayDate = null,
        int? DayIndex = null)
    {
        return new TripDay
        {
            Id = Guid.NewGuid(),
            TripId = TripId,
            Title = Tittle,
            DayDate = DayDate,
            DayIndex = DayIndex ?? 0,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void Update(string? title = null, DateTime? dayDate = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        if (dayDate.HasValue)
        {
            // Convert to UTC to match database timezone
            var dateOnly = dayDate.Value.Date;
            DayDate = dateOnly.Kind == DateTimeKind.Utc
                ? dateOnly
                : DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
        }

        ModifiedDate = DateTime.UtcNow;
    }
}