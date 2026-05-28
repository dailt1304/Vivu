using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class TripLocationAlternative : Entity<Guid>
{
    public Guid TripLocationId { get; set; }
    public Guid LocationId { get; set; }
    public int Priority { get; set; } = 1;
    public string? Reason { get; set; }

    public virtual TripLocation TripLocation { get; set; } = null!;
    public virtual Location Location { get; set; } = null!;

    public static TripLocationAlternative Create(
        Guid tripLocationId, Guid locationId, int priority, string? reason = null)
    {
        return new TripLocationAlternative
        {
            Id = Guid.NewGuid(),
            TripLocationId = tripLocationId,
            LocationId = locationId,
            Priority = priority,
            Reason = reason,
            CreatedDate = DateTime.UtcNow
        };
    }
}
