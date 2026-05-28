namespace Vivu.Application.DTOs.Responses.Destinations
{
    public class TrendingDestinationDto
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int TripCount { get; set; }
        public List<TrendingLocationDto> TrendingLocations { get; set; } = new();
    }
}
