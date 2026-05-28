using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay
{
    public class ReorderTripDayCommand : IRequest<Result<List<TripDayResponse>>>
    {
        public Guid TripId { get; set; }
        public List<Guid> OrderedTripDayIds { get; set; } = new();
    }
}
