using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.ApplyTripModification
{
    public record ApplyTripModificationCommand : IRequest<Result<DetailedTripDto>>
    {
        public required Guid TripId { get; init; }
        public required TripModificationResponse Modification { get; init; }
        public required string UserRequest { get; init; }
    }
}
