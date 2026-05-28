using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTripDetail
{
    public class GetPublicTripDetailQueryValidator : AbstractValidator<GetPublicTripDetailQuery>
    {
        public GetPublicTripDetailQueryValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");
        }
    }
}
