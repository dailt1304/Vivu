using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.CreateTrip
{
    public class CreateTripCommand : IRequest<Result<TripDto>>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverUrl { get; set; }
        public Guid? CityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TripSize { get; set; }
        public bool IsPublic { get; set; } = false;
        public bool GenerateInviteCode { get; set; } = true;
    }
}
