using FluentValidation;

namespace Vivu.Application.UseCases.Collections.Commands.UpdateCollection
{
    public class UpdateCollectionCommandValidator : AbstractValidator<UpdateCollectionCommand>
    {
        public UpdateCollectionCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Collection name is required.")
                .MaximumLength(200).WithMessage("Collection name must not exceed 200 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(v => v.CoverImage)
                .Must(i => i == null || i.Length <= 5 * 1024 * 1024)
                .WithMessage("Cover image size must not exceed 5MB.");
        }
    }
}
