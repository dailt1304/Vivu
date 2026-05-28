using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class RateLimitCheckResult
    {
        public int Remaining { get; set; }
        public int Limit { get; set; }
        public DateTime ResetAt { get; set; }
    }
}
