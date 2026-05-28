using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers
{
    public class GetTripMembersQuery : PaginationRequest, IRequest<Result<PaginatedList<TripMemberDto>>>
    {
        public Guid TripId { get; set; }
    }
}
