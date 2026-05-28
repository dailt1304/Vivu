using System;
using System.Collections.Generic;

namespace Vivu.Application.DTOs.Responses.Statistics
{
    public class LocationStatsDto
    {
        public Dictionary<string, int> LocationsByCategory { get; set; } = new Dictionary<string, int>();
        public int PendingSubmissionsCount { get; set; }
        public Dictionary<string, int> ReportsByType { get; set; } = new Dictionary<string, int>();
        public decimal VerificationRate { get; set; }
        public int TotalVerifiedLocations { get; set; }
    }
}
