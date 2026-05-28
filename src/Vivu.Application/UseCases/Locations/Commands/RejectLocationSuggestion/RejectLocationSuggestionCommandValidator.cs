using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion
{
    public class RejectLocationSuggestionCommandValidator : AbstractValidator<RejectLocationSuggestionCommand>
    {
        public RejectLocationSuggestionCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .NotEmpty()
                .WithMessage("Location ID is required.");

            RuleFor(x => x.AdminNote)
                .NotEmpty()
                .WithMessage("Admin note is required.");
        }
    }
}
