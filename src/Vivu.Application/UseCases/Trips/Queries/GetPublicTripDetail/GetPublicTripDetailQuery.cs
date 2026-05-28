using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTripDetail
{
    public class GetPublicTripDetailQuery : IRequest<Result<DetailedTripDto>>
    {
        public Guid TripId { get; set; }
    }
}
