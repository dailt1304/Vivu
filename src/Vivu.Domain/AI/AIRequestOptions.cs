using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.AI
{
    public class AIRequestOptions
    {
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
        public string? Model { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }
}
