using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.SearchCities
{
    public class SearchCitiesQuery : PaginationRequest, IRequest<Result<PaginatedList<CityDto>>>
    {
        public string SearchText { get; set; } = string.Empty;
    }
}
