using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamChatWithAI
{
    public record StreamChatWithAICommand : IStreamRequest<Result<StreamEvent>>
    {
        public required Guid TripId { get; init; }
        public required string UserMessage { get; init; }
    }
}
