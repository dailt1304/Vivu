using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations
{
    public class ReorderTripLocationsCommand : IRequest<Result<List<TripLocationResponse>>>
    {
        public Guid TripDayId { get; set; }
        public List<Guid> OrderedTripLocationIds { get; set; } = new();
    }
}
