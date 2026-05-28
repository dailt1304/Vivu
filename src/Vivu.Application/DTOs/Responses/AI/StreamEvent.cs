using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Enums;

namespace Vivu.Application.DTOs.Responses.AI
{
    public class StreamEvent
    {
        public StreamEventType Type { get; set; }
        public object? Data { get; set; }
    }
}
