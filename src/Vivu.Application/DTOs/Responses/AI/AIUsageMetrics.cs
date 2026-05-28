using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class AIUsageMetrics
    {
        public int TokensInput { get; set; }
        public int TokensOutput { get; set; }
        public int TokensTotal => TokensInput + TokensOutput;
        public int ResponseTimeMs { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Provider { get; set; } = string.Empty;
    }
}
