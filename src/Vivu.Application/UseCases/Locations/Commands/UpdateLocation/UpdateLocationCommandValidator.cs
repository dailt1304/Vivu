using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Commands.UpdateLocation
{
    public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
    {
        public UpdateLocationCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .NotEmpty()
                .WithMessage("Location ID is required.");

            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Address));

            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters.")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Website)
                .MaximumLength(500)
                .WithMessage("Website must not exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Website));
        }
    }
}
