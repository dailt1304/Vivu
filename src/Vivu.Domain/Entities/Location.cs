using Vivu.Domain.Shared;
using NetTopologySuite.Geometries;

namespace Vivu.Domain.Entities;

public class Location : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Guid? CityId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsVerified { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public Point? LocationPoint { get; set; }

    // Navigation Properties
    public virtual City? City { get; set; }
    public virtual LocationCategory? Category { get; set; }
    public virtual LocationDetail? LocationDetail { get; set; }
    public virtual ICollection<TripLocation> TripLocations { get; set; } = new List<TripLocation>();
    public virtual ICollection<LocationReport> LocationReports { get; set; } = new List<LocationReport>();
    public virtual ICollection<CollectionLocation> CollectionLocations { get; set; } = new List<CollectionLocation>();

    public static Location Create(
        string name,
        string? description,
        string? address,
        double? latitude,
        double? longitude,
        Guid? cityId,
        Guid? categoryId,
        bool isVerified = false)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            CityId = cityId,
            CategoryId = categoryId,
            RatingAverage = 0,
            RatingCount = 0,
            IsVerified = isVerified,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        // Create LocationPoint from coordinates if provided
        if (latitude.HasValue && longitude.HasValue)
        {
            location.LocationPoint = new Point(
                longitude.Value,  // X = longitude
                latitude.Value)   // Y = latitude
            {
                SRID = 4326
            };
        }

        return location;
    }

    public void Update(string? name = null, string? description = null, string? address = null, double? latitude = null, double? longitude = null, Guid? categoryId = null, string? images = null, bool? isVerified = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
        
        if (description != null)
            Description = description;
        
        if (address != null)
            Address = address;
        
        if (latitude.HasValue)
            Latitude = latitude.Value;
        
        if (longitude.HasValue)
            Longitude = longitude.Value;
        
        if (categoryId.HasValue)
            CategoryId = categoryId.Value;
        
        if (!string.IsNullOrWhiteSpace(images) && LocationDetail != null)
            LocationDetail.Images = images;
        
        if (isVerified.HasValue)
            IsVerified = isVerified.Value;
    }
    public double GetDistanceInMeters(Point locationPoint)
    {
        return LocationPoint!.Distance(locationPoint);
    }

    public void Delete()
    {
        IsDeleted = true;
        ModifiedDate = DateTime.UtcNow;
    }
}
