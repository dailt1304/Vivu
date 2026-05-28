using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.Trips.Commands.CopyTrip
{
    public class CopyTripCommandValidator : AbstractValidator<CopyTripCommand>
    {
        public CopyTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Title));

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Start date must be in the future")
                .When(x => x.StartDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.TripSize)
                .GreaterThan(0)
                .WithMessage("Trip size must be greater than 0")
                .When(x => x.TripSize.HasValue);
        }
    }
}
