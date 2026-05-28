using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.UnfavoriteTrip
{
    public class UnfavoriteTripCommandValidator : AbstractValidator<UnfavoriteTripCommand>
    {
        public UnfavoriteTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.")
                .NotEqual(Guid.Empty)
                .WithMessage("Trip ID must be a valid GUID.");
        }
    }
}
