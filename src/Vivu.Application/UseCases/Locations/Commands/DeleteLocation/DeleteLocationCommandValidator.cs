using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Commands.DeleteLocation
{
    public class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
    {
        public DeleteLocationCommandValidator()
        {
            RuleFor(x => x.LocationId)
                .NotEmpty()
                .WithMessage("Location ID is required.");
        }
    }
}
