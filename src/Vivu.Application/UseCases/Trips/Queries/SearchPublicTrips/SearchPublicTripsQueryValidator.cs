using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.SearchPublicTrips
{
    public class SearchPublicTripsQueryValidator : AbstractValidator<SearchPublicTripsQuery>
    {
        public SearchPublicTripsQueryValidator()
        {
            RuleFor(x => x.SearchTerm)
                .NotEmpty()
                .WithMessage("Search term is required")
                .MinimumLength(2)
                .WithMessage("Search term must be at least 2 characters");

            // PageNumber and PageSize validation rules are not needed here because
            // PaginationRequest clamps these values in the property setters:
            // - PageNumber < 1 is clamped to 1
            // - PageSize < 1 is clamped to 10
            // - PageSize > 100 is clamped to 100
        }
    }
}
