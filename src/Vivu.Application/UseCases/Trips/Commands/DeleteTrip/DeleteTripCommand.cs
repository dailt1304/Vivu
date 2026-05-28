using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.DeleteTrip
{
    public class DeleteTripCommand : IRequest<Result<bool>>
    {
        public Guid TripId { get; set; }
    }
}
