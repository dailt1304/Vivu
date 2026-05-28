using MediatR;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.UpdateLocation
{
    public class UpdateLocationCommand : IRequest<Result<LocationDto>>
    {
        public Guid LocationId { get; set; }
        
        // Basic Info
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public Guid? CategoryId { get; set; }
        
        // Coordinates
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Location Details
        public string? OpeningHours { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Tags { get; set; }
        public string? Images { get; set; }
        
        // Admin
        public bool? IsVerified { get; set; }
    }
}
