using FluentValidation;

namespace Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers
{
    public class GetTripMembersQueryValidator : AbstractValidator<GetTripMembersQuery>
    {
        public GetTripMembersQueryValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty().WithMessage("Trip ID is required");
        }
    }
}
