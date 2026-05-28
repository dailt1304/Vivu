using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay
{
    public class UpdateTripDayValidator : AbstractValidator<UpdateTripDayCommand>
    {
        public UpdateTripDayValidator()
        {
            RuleFor(x => x.TripDayId)
                .NotEmpty()
                .WithMessage("TripDayId is required.");

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("Title must be at most 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Title));
        }
    }
}
