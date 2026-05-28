using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.FavouriteTrip
{
    public record FavoriteTripCommand(Guid TripId) : IRequest<Result<Unit>>;

}
