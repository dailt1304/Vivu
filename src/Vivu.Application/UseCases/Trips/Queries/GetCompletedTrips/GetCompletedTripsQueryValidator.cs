using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.GetCompletedTrips
{
    public class GetCompletedTripsQueryValidator : AbstractValidator<GetCompletedTripsQuery>
    {
        public GetCompletedTripsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}
