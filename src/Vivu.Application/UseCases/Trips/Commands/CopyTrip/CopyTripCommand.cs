using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.CopyTrip
{
    public class CopyTripCommand : IRequest<Result<TripDto>>
    {
        public Guid TripId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? TripSize { get; set; }
        public string? CoverUrl { get; set; }
    }
}
