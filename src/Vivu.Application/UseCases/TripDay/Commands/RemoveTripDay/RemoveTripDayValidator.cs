using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay
{
    public class RemoveTripDayValidator : AbstractValidator<RemoveTripDayCommand>
    {
        public RemoveTripDayValidator()
        {
            RuleFor(x => x.TripDayId)
                .NotEmpty().WithMessage("TripDayId is required");
        }
    }
}
