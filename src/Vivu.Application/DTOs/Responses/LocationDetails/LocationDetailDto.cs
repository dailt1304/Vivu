using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.LocationDetails
{
    public class LocationDetailDto
    {
        public Guid LocationId { get; set; }
        public string? OpeningHours { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Tags { get; set; }
        public string? Images { get; set; }
    }
}
