using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTripStatus
{
    public class UpdateTripStatusCommand : IRequest<Result<int>>
    {
        // No properties needed 
    }
}
