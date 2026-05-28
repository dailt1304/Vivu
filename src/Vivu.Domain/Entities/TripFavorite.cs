namespace Vivu.Domain.Entities;

public class TripFavorite
{
    public Guid UserId { get; set; }
    public Guid TripId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Trip Trip { get; set; } = null!;
}
