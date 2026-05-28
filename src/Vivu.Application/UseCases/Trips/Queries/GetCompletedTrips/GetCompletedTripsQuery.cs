using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetCompletedTrips
{
    public class GetCompletedTripsQuery : PaginationRequest, IRequest<Result<PaginatedList<TripDto>>>
    {
        public Guid UserId { get; set; }
    }
}
