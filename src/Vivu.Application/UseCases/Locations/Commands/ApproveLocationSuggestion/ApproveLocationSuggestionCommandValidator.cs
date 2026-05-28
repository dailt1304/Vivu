using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion
{
    public class ApproveLocationSuggestionCommandValidator : AbstractValidator<ApproveLocationSuggestionCommand>
    {
        public ApproveLocationSuggestionCommandValidator()
        {
            RuleFor(x => x.LocationId)
               .NotEmpty()
               .WithMessage("LocationId is required");
        }
    }
}
