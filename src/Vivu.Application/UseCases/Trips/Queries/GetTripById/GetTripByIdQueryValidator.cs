using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.GetTripById
{
    public class GetTripByIdQueryValidator : AbstractValidator<GetTripByIdQuery>
    {
        public GetTripByIdQueryValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");

            RuleFor(x => x.RequestUserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}
