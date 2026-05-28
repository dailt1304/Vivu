using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.RateTrip
{
    public class RateTripCommandValidator : AbstractValidator<RateTripCommand>
    {
        public RateTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty().WithMessage("Trip ID is required");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

            RuleFor(x => x.ReviewContent)
                .MaximumLength(1000).WithMessage("Review content must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.ReviewContent));
        }
    }
}
