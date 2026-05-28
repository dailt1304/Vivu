using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.LocationCategories
{
    public class LocationCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int CategoryType { get; set; }
        public bool IsActive { get; set; }
        public int LocationCount { get; set; }
    }
}
