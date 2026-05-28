using Vivu.Application.DTOs.Responses.Locations;

namespace Vivu.Application.DTOs.Responses.Trips
{
    public class TripLocationDto
    {
        public Guid Id { get; set; }
        public int DayNumber { get; set; }
        public int OrderIndex { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Note { get; set; }
        public string? TransportMode { get; set; }
        public LocationDto Location { get; set; } = null!;
        public List<TripLocationAlternativeDto>? Alternatives { get; set; }
    }
}
