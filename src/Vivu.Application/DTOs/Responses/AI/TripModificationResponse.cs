using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripModificationResponse
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("changes")]
        public List<TripChange> Changes { get; set; } = new();
    }

}
