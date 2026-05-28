using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.DeleteTrip
{
    public class DeleteTripCommandValidator : AbstractValidator<DeleteTripCommand>
    {
        public DeleteTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");
        }
    }
}
