using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamAIChat
{
    public record StreamAIDetectIntentCommand : IStreamRequest<Result<StreamEvent>>
    {
        public Guid TripId { get; set; }
        public required string UserMessage { get; init; }
        public bool SkipSavingUserMessage { get; init; } = false;
    }
}
