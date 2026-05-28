using FluentValidation;

namespace Vivu.Application.UseCases.Collections.Commands.DeleteCollection
{
    public class DeleteCollectionCommandValidator : AbstractValidator<DeleteCollectionCommand>
    {
        public DeleteCollectionCommandValidator()
        {
            RuleFor(x => x.CollectionId).NotEmpty().WithMessage("CollectionId is required.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        }
    }
}
