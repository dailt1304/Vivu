using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.Interfaces.AI
{
    public interface IAIClient
    {
        string ProviderName { get; }

        Task<Result<AIRawResponse>> SendRequestAsync(
            string prompt,
            AIRequestOptions options,
            CancellationToken cancellationToken = default
        );
        IAsyncEnumerable<Result<AIStreamChunk>> SendStreamRequestAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}
