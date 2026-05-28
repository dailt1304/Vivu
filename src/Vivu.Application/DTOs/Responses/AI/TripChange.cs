using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripChange
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("dayIndex")]
        public int DayIndex { get; set; }

        [JsonPropertyName("location")]
        public TripLocationChange? Location { get; set; }

        [JsonPropertyName("locationId")]
        public Guid? LocationId { get; set; }

        [JsonPropertyName("oldLocationId")]
        public Guid? OldLocationId { get; set; }

        [JsonPropertyName("startTime")]
        public TimeOnly? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public TimeOnly? EndTime { get; set; }

        [JsonPropertyName("date")]
        public DateOnly? Date { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("locations")]
        public List<TripLocationChange>? Locations { get; set; }
    }
}
