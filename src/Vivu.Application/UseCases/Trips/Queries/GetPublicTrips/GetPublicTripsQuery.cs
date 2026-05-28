using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTrips
{
    public class GetPublicTripsQuery : PaginationRequest, IRequest<Result<PaginatedList<PublicTripDto>>>
    {
        public Guid? CityId { get; set; }
        public Guid? CountryId { get; set; }
        public int? Duration { get; set; }  
        public string SortBy { get; set; } = "newest"; 
    }
}
