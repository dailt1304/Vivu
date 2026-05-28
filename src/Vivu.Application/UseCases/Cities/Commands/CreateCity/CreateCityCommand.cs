using MediatR;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.CreateCity
{
    public record CreateCityCommand : IRequest<Result<CityDto>>
    {
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Image { get; set; }
    }
}
