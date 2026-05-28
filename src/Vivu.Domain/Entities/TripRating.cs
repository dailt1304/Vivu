using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class TripRating : Entity<Guid>
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? ReviewContent { get; set; }

    // Navigation Properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    public static TripRating Create(Guid tripId, Guid userId, int rating, string? reviewContent)
    {
        var tripRating = new TripRating
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            UserId = userId,
            Rating = rating,
            ReviewContent = reviewContent,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        return tripRating;
    }
}
