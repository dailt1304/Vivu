using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility
{
    public class UpdateTripVisibilityCommand : IRequest<Result<TripDto>>
    {
        public Guid TripId { get; set; }
        public bool IsPublic { get; set; }
    }
}
