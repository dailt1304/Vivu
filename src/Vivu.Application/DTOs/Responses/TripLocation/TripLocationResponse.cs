using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.TripLocation
{
    public class TripLocationResponse
    {
        public Guid Id { get; set; }
        public Guid TripDayId { get; set; }
        public Guid LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? LocationAddress { get; set; }
        public int OrderIndex { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Note { get; set; }
        public string? TransportMode { get; set; }
        public string? Images { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
