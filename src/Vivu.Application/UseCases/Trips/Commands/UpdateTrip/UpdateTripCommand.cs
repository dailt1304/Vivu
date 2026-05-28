using MediatR;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTrip
{
    public class UpdateTripCommand : IRequest<Result<TripDto>>
    {
        public Guid TripId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CoverUrl { get; set; }
        public int? TripSize { get; set; }
        public string Status { get; set; } = "planning";
    }
}
