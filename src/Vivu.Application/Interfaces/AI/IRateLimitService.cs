using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.Interfaces.AI
{
    public interface IRateLimitService
    {
        Task<Result<RateLimitCheckResult>> CheckLimitAsync(
            Guid userId,
            CancellationToken cancellationToken = default
        );
    }
}
