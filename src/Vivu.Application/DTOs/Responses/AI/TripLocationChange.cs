using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripLocationChange
    {
        [JsonPropertyName("locationId")]
        public Guid LocationId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("startTime")]
        public TimeOnly StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public TimeOnly EndTime { get; set; }

        [JsonPropertyName("transportMode")]
        public string? TransportMode { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("alternatives")]
        public List<AlternativeLocationDto>? Alternatives { get; set; }
    }
}
