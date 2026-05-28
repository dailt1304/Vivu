namespace Vivu.Application.DTOs.Responses.TripLocation
{
    public class TripLocationAlternativeResponse
    {
        public Guid Id { get; set; }
        public Guid TripLocationId { get; set; }
        public Guid LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? LocationAddress { get; set; }
        public string? Images { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Priority { get; set; }
        public string? Reason { get; set; }
    }
}
