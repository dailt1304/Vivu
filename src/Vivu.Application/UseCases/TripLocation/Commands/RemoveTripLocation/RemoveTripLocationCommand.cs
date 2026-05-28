using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation
{
    public class RemoveTripLocationCommand : IRequest<Result<TripLocationResponse>>
    {
        public Guid TripLocationId { get; set; }
    }
}
