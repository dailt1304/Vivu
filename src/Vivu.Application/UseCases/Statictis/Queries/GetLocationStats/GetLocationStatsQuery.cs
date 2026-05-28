using MediatR;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Statictis.Queries.GetLocationStats
{
    public class GetLocationStatsQuery : IRequest<Result<LocationStatsDto>>
    {
    }
}
