namespace Vivu.Domain.Entities;

public class LocationDetail
{
    public Guid LocationId { get; set; }
    public string? OpeningHours { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Tags { get; set; }
    public string? Images { get; set; } // Will be configured as jsonb in PostgreSQL

    // Navigation Properties
    public virtual Location Location { get; set; } = null!;

    public static LocationDetail Create(
        Guid locationId,
        string? openingHours = null,
        string? phone = null,
        string? website = null,
        string? tags = null,
        string? images = null)
    {
        return new LocationDetail
        {
            LocationId = locationId,
            OpeningHours = openingHours,
            Phone = phone,
            Website = website,
            Tags = tags,
            Images = images
        };
    }

    public void Update(string? openingHours = null, string? phone = null, string? website = null, string? tags = null, string? images = null)
    {
        if (openingHours != null)
            OpeningHours = openingHours;
        
        if (phone != null)
            Phone = phone;
        
        if (website != null)
            Website = website;
        
        if (tags != null)
            Tags = tags;
        
        if (images != null)
            Images = images;
    }
}
