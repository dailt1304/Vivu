using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class DayPlanDto
    {
        public int DayIndex { get; set; }
        public DateOnly Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<LocationPlanDto> Locations { get; set; } = new();
    }
}
