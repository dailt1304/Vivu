using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class TripConstraints
    {
        public TimeOnly? StartTimeEachDay { get; set; }
        public TimeOnly? EndTimeEachDay { get; set; }
        public List<string> MustIncludeTypes { get; set; } = new();
        public List<string> MustExcludeTypes { get; set; } = new();
        public List<string> MustIncludeKeywords { get; set; } = new();
        public int? MaxLocationsPerDay { get; set; }
        public int? MinLocationsPerDay { get; set; }
        public string? PaceStyle { get; set; }
        public List<string> FreeformRules { get; set; } = new();

        public bool HasAnyConstraint =>
            StartTimeEachDay.HasValue ||
            EndTimeEachDay.HasValue ||
            MustIncludeTypes.Any() ||
            MustExcludeTypes.Any() ||
            MustIncludeKeywords.Any() ||
            MaxLocationsPerDay.HasValue ||
            MinLocationsPerDay.HasValue ||
            !string.IsNullOrWhiteSpace(PaceStyle) ||
            FreeformRules.Any();
    }
}
