using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations
{
    public class ReorderTripLocationValidator : AbstractValidator<ReorderTripLocationsCommand>
    {
        public ReorderTripLocationValidator()
        {
            RuleFor(x => x.TripDayId)
                .NotEmpty()
                .WithMessage("TripDayId is required.");

            RuleFor(x => x.OrderedTripLocationIds)
                .NotNull()
                .WithMessage("OrderedTripLocationIds is required.")
                .NotEmpty()
                .WithMessage("OrderedTripLocationIds must not be empty.");

            RuleForEach(x => x.OrderedTripLocationIds)
                .NotEmpty()
                .WithMessage("TripLocationId must not be empty.");

            RuleFor(x => x.OrderedTripLocationIds)
                .Must(list => list.Distinct().Count() == list.Count)
                .WithMessage("OrderedTripLocationIds contains duplicate ids.");
        }
    }
}
