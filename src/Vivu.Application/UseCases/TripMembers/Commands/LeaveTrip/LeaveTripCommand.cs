using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip
{
    public class LeaveTripCommand : IRequest<Result<bool>>
    {
        public Guid TripId { get; set; }
    }
}
