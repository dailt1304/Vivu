using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility
{
    public class UpdateTripVisibilityCommandValidator : AbstractValidator<UpdateTripVisibilityCommand>
    {
        public UpdateTripVisibilityCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");
        }
    }
}
