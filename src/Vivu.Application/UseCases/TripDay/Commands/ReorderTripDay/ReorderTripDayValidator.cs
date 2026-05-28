using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay
{
    public class ReorderTripDayValidator : AbstractValidator<ReorderTripDayCommand>
    {
        public ReorderTripDayValidator()
        {
            RuleFor(x => x.TripId)
                   .NotEmpty().WithMessage("TripId is required");

            RuleFor(x => x.OrderedTripDayIds)
               .NotNull().WithMessage("OrderedTripDayIds is required.")
               .NotEmpty().WithMessage("OrderedTripDayIds must not be empty.");

            RuleForEach(x => x.OrderedTripDayIds)
                .NotEmpty()
                .WithMessage("TripDayId must not be empty.");

            RuleFor(x => x.OrderedTripDayIds)
                .Must(list => list.Distinct().Count() == list.Count)
                .WithMessage("OrderedTripDayIds contains duplicate ids.");
        }
    }
}
