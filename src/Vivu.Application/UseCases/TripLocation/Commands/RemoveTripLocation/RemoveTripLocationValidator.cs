using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation
{
    public class RemoveTripLocationValidator : AbstractValidator<RemoveTripLocationCommand>
    {
        public RemoveTripLocationValidator()
        {
            RuleFor(x => x.TripLocationId)
                .NotEmpty().WithMessage("TripLocationId is required");
        }
    }
}
