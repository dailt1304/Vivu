using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Destinations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Destinations.Queries.GetTrendingDestinations
{
    public class GetTrendingDestinationsQuery : PaginationRequest, IRequest<Result<PaginatedList<TrendingDestinationDto>>>
    {
        public int LocationsPerCity { get; set; } = 10;
    }
}
