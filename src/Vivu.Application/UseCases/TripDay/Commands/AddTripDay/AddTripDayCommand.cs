using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.AddTripDay
{
    public class AddTripDayCommand : IRequest<Result<TripDayResponse>>
    {   
        public Guid TripId { get; set; }
        public string? Title { get; set; }
        public int? DayIndex { get; set; }
        public DateTime? DayDate { get; set; }
    }
}
