using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.UpdateTripLocation
{
    public class UpdateTripLocationCommand : IRequest<Result<TripLocationResponse>>
    {
        public Guid TripLocationId { get; set; }
        
        public Guid TripDayId { get; set; }

        public Guid LocationId { get; set; }

        public int OrderIndex { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string? Note { get; set; }

        public string? TransportMode { get; set; }
    }
}
