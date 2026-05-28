using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.DTOs.Responses.LocationDetails;

namespace Vivu.Application.DTOs.Responses.Locations
{
    public class LocationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? CityId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal RatingAverage { get; set; }
        public bool IsVerified { get; set; }
        public double? DistanceInMeters { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RatingCount { get; set; }
        public CityDto? City { get; set; }
        public LocationCategoryDto? Category { get; set; }
        public LocationDetailDto? LocationDetail { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<NearbyLocationDto> NearbyLocations { get; set; } = new();
        public Guid? SubmittedByUserId { get; set; }
        public string? SubmittedByUserName { get; set; }
        public string? SubmittedByUserEmail { get; set; }
        public string? SubmittedByUserAvatar { get; set; }
    }
}
