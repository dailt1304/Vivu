using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class TripLocation : Entity<Guid>
{
    public const int MaxAlternatives = 2;

    public Guid TripDayId { get; set; }
    public Guid LocationId { get; set; }
    public int OrderIndex { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Note { get; set; }
    public string? TransportMode { get; set; }

    // Navigation Properties
    public virtual TripDay TripDay { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<TripLocationAlternative> Alternatives { get; set; } = new List<TripLocationAlternative>();

    public static TripLocation Create(
        Guid tripDayId,
        Guid locationId,
        int orderIndex,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? note = null,
        string? transportMode = null)
    {
        return new TripLocation
        {
            Id = Guid.NewGuid(),
            TripDayId = tripDayId,
            LocationId = locationId,
            OrderIndex = orderIndex,
            StartTime = startTime,
            EndTime = endTime,
            Note = note,
            TransportMode = transportMode,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void Update(
        Guid tripDayId,
        Guid locationId,
        int orderIndex,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? note = null,
        string? transportMode = null)
    {
        TripDayId = tripDayId;
        LocationId = locationId;
        OrderIndex = orderIndex;
        StartTime = startTime;
        EndTime = endTime;
        Note = note;
        TransportMode = transportMode;
        ModifiedDate = DateTime.UtcNow;
    }
}