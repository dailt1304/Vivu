using FluentValidation;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTrip
{
    public class UpdateTripCommandValidator : AbstractValidator<UpdateTripCommand>
    {
        public UpdateTripCommandValidator()
        {
            RuleFor(x => x.TripId)
                .NotEmpty()
                .WithMessage("Trip ID is required.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(status => status != null && new[] { "planning", "ongoing", "completed" }.Contains(status.ToLower()))
                .WithMessage("Status must be one of: planning, ongoing, completed.");

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate.Value < x.EndDate.Value)
                .WithMessage("Start date must be earlier than end date.")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }
}
