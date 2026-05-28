using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Enums;

namespace Vivu.Application.Interfaces.AI
{
    public interface IIntentDetectionService
    {
        Task<AIChatIntent> DetectIntentAsync(
            string userMessage,
            CancellationToken cancellationToken = default);
    }
}
