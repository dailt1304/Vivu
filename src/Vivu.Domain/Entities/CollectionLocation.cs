using System;

namespace Vivu.Domain.Entities
{
public class CollectionLocation
{
    public Guid CollectionId { get; set; }
    public Guid LocationId { get; set; }
    public string? Note { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation
    public virtual Collection Collection { get; set; } = null!;
    public virtual Location Location { get; set; } = null!;
        
        private CollectionLocation() { } // EF Core
        
        public static CollectionLocation Create(Guid collectionId, Guid locationId, string? note = null)
        {
            return new CollectionLocation
            {
                CollectionId = collectionId,
                LocationId = locationId,
                Note = note,
                AddedAt = DateTime.UtcNow
            };
        }
    }
}
