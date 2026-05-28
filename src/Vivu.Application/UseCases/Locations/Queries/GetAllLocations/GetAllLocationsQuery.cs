using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetAllLocations
{
    public class GetAllLocationsQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationDto>>>
    {
    }
}
