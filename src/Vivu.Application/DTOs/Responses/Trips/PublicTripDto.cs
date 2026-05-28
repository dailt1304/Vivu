using Vivu.Application.DTOs.Responses.Users;

namespace Vivu.Application.DTOs.Responses.Trips
{
    public class PublicTripDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TripSize { get; set; }
        public string Status { get; set; } = "planning";
        public DateTime CreatedAt { get; set; }
        
        // Owner information
        public UserDto? Owner { get; set; }
        
        // Aggregated data
        public int MemberCount { get; set; }
        public int FavoritesCount { get; set; }
        
        // City/Country information
        public string? CityName { get; set; }
        public string? CountryName { get; set; }
        
        // Duration in days
        public int? DurationDays { get; set; }
        
        // Trending score (for sorting)
        public double? TrendingScore { get; set; }
    }
}
