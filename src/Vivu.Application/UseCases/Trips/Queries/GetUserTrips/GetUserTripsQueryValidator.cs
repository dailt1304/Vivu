using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Queries.GetUserTrips
{
    public class GetUserTripsQueryValidator : AbstractValidator<GetUserTripsQuery>
    {
        private static readonly string[] ValidStatuses = { "planning", "ongoing", "completed" };

        public GetUserTripsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.Status)
                .Must(status => string.IsNullOrWhiteSpace(status) || ValidStatuses.Contains(status.ToLower()))
                .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        }
    }
}
