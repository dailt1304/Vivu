namespace Vivu.Application.DTOs.Responses.Trips
{
    public class TripDayDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public DateTime? DayDate { get; set; }
        public int DayIndex { get; set; }
        public List<TripLocationDto> Locations { get; set; } = new List<TripLocationDto>();
    }
}
