using Vivu.Domain.Enums;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripPlanDto
    {
        public Guid UserId { get; set; }
        public Guid CityId { get; set; }
        public string Destination { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int GroupSize { get; set; } = 1;
        public List<string> Preferences { get; set; } = [];
        public TripBudget? Budget { get; set; }
        public string? Notes { get; set; }
        public string? Title { get; set; }

        public UserPersonalizationContext? Personalization { get; set; }
    }
}
