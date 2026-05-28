using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripPlanResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }
        public int Size { get; set; }
        public List<DayPlanDto> Days { get; set; } = new();
        public AIUsageMetrics UsageMetrics { get; set; } = new();
    }
   
}
