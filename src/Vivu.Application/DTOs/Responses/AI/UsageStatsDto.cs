using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class UsageStatsDto
    {
        public int Used { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTime ResetAt { get; set; }
        public bool HasActiveSubscription { get; set; }
        public string? PackageName { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}
