using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.RateTrip
{
    public class RateTripCommand : IRequest<Result<bool>>
    {
        public Guid TripId { get; set; }
        public int Rating { get; set; }
        public string? ReviewContent { get; set; }
    }
}
