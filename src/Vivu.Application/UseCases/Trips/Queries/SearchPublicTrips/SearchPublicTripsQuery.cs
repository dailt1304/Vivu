using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.SearchPublicTrips
{
    public class SearchPublicTripsQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicTripDto>>>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public Guid? CityId { get; set; }
        public Guid? CountryId { get; set; }
        public int? Duration { get; set; }
    }
}
