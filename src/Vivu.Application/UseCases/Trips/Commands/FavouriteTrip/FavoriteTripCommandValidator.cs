using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.FavouriteTrip
{
    public class FavoriteTripCommandValidator : AbstractValidator<FavoriteTripCommand>
    {
        public FavoriteTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.")
                .NotEqual(Guid.Empty)
                .WithMessage("Trip ID must be a valid GUID.");
        }
    }
}
