using FluentValidation;

namespace Vivu.Application.UseCases.Collections.Commands.AddLocationToCollection
{
    public class AddLocationToCollectionCommandValidator : AbstractValidator<AddLocationToCollectionCommand>
    {
        public AddLocationToCollectionCommandValidator()
        {
            RuleFor(x => x.CollectionId).NotEmpty().WithMessage("CollectionId is required.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
            RuleFor(x => x.LocationId).NotEmpty().WithMessage("LocationId is required.");
            RuleFor(x => x.Note).MaximumLength(500).WithMessage("Note must not exceed 500 characters.");
        }
    }
}
