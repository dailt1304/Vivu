using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Countries;

namespace Vivu.Application.DTOs.Responses.Cities
{
    public class CityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameAscii { get; set; }
        public Guid CountryId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Image { get; set; }
        public CountryDto? Country { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int LocationCount { get; set; }
    }
}