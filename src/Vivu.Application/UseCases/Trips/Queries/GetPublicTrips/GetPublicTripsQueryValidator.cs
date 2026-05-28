using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTrips
{
    public class GetPublicTripsQueryValidator : AbstractValidator<GetPublicTripsQuery>
    {
        public GetPublicTripsQueryValidator()
        {
            RuleFor(x => x.Duration)
                .GreaterThan(0)
                .When(x => x.Duration.HasValue)
                .WithMessage("Duration must be a positive number");

            RuleFor(x => x.SortBy)
                .Must(s => s == null || s == "newest" || s == "popular" || s == "trending")
                .WithMessage("SortBy must be 'newest', 'popular', or 'trending'");
        }
    }
}
