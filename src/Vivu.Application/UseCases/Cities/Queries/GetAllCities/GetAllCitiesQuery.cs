using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.GetAllCities
{
    public class GetAllCitiesQuery : PaginationRequest, IRequest<Result<PaginatedList<CityDto>>>
    {
        public Guid? CountryId { get; set; }
    }
}
