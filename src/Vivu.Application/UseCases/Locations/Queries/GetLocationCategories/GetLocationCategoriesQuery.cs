using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetLocationCategories
{
    public class GetLocationCategoriesQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationCategoryDto>>>
    {
    }
}
