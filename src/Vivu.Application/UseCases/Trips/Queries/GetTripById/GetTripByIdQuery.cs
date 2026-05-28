using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetTripById
{
    public class GetTripByIdQuery : IRequest<Result<DetailedTripDto>>
    {
        public Guid TripId { get; set; }
        public Guid RequestUserId { get; set; }
    }
}
