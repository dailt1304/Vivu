using FluentValidation;

namespace Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip
{
    public class LeaveTripCommandValidator : AbstractValidator<LeaveTripCommand>
    {
        public LeaveTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty().WithMessage("Trip ID is required");
        }
    }
}
