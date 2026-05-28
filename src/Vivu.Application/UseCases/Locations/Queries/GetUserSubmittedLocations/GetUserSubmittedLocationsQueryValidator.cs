using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Queries.GetUserSubmittedLocations
{
    public class GetUserSubmittedLocationsQueryValidator : AbstractValidator<GetUserSubmittedLocationsQuery>
    {
        public GetUserSubmittedLocationsQueryValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("Status must be a valid ReportStatus (PENDING, APPROVED, REJECTED)");
        }
    }
}
