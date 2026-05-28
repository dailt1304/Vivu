using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay
{
    public class RemoveTripDayCommand : IRequest<Result<TripDayResponse>>
    {
        public Guid TripDayId { get; set; }
    }
}
