using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class LocationPlanDto
    {
        public Guid LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? TransportMode { get; set; }
        public int OrderIndex { get; set; }
        public List<AlternativeLocationDto> Alternatives { get; set; } = new();
    }
}
