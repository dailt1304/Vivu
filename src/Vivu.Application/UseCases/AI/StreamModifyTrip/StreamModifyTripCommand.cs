using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.StreamModifyTrip
{
    public record StreamModifyTripCommand : IStreamRequest<Result<StreamEvent>>
    {
        public required Guid TripId { get; init; }
        public required string UserRequest { get; init; }
    }
}
