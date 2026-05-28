using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.CreateLocation
{
    public class CreateLocationCommand : IRequest<Result<LocationDto>>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? CityId { get; set; }
        public Guid? CategoryId { get; set; }
        public string? OpeningHours { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Tags { get; set; }
        public List<IFormFile> Images { get; set; } = new();
    }
}
