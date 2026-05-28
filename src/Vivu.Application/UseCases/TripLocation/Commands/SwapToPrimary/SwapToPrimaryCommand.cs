using MediatR;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.SwapToPrimary
{
    public class SwapToPrimaryCommand : IRequest<Result<TripLocationResponse>>
    {
        public Guid TripLocationId { get; set; }
        public Guid AlternativeId { get; set; }
    }
}
