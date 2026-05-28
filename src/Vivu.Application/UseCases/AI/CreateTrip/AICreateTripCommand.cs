using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.AI.CreateTrip
{
    public record AICreateTripCommand : IRequest<Result<DetailedTripDto>>
    {
        public required TripPlanResponse TripPlan { get; init; }
        public string? UserPrompt { get; init; }
        public string? CoverUrl { get; init; }
        public required Guid cityId { get; init; }
        public bool GenerateInviteCode { get; init; } = true;
        public string? PersonalizationContextJson { get; init; }
        public string? ConstraintsJson { get; init; }
    }
}
