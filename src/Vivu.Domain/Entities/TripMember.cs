namespace Vivu.Domain.Entities;

public class TripMember
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; }
    public Guid OwnerId { get; set; }

    // Navigation Properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User Owner { get; set; } = null!;

    public static TripMember Create(Guid tripId, Guid userId, Guid ownerId, string role = "owner")
    {
        return new TripMember
        {
            TripId = tripId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            OwnerId = ownerId
        };
    }
}
