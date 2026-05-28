using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.AI;

namespace Vivu.Application.Interfaces.AI
{
    public interface IUsageTrackingService
    {
        Task LogUsageAsync(
            Guid userId,
            Guid? subscriptionId,
            string provider,
            int tokensInput,
            int tokensOutput,
            int responseTimeMs,
            decimal totalCost,
            int statusCode = 200,
            string? errorMessage = null,
            CancellationToken cancellationToken = default
        );
        Task LogFailedUsageAsync(
            Guid userId,
            Guid? subscriptionId,
            string errorMessage,
            AIRawResponse? response = null);
        decimal CalculateCost(int tokeninput, int tokenoutput, string provider);
    }
}
