using MediatR;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.UpdateCity
{
    public record UpdateCityCommand : IRequest<Result<CityDto>>
    {
        public Guid CityId { get; set; }
        public string? Name { get; set; }
        public Guid? CountryId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Image { get; set; }
    }
}
