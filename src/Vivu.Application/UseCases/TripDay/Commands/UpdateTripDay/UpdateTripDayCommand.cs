using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay
{
    public class UpdateTripDayCommand : IRequest<Result<TripDayResponse>>
    {
        public Guid TripDayId { get; set; }
        public string? Title { get; set; }
        public DateTime? DayDate { get; set; }
    }
}
