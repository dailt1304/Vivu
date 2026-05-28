using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class AIStreamChunk
    {
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string ProviderName { get; set; }
        public int? ResponseTimeMs { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
    }

}
