using MediatR;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Queries.GetCityById
{
    public class GetCityByIdQuery : IRequest<Result<CityDto>>
    {
        public Guid CityId { get; set; }
    }
}
