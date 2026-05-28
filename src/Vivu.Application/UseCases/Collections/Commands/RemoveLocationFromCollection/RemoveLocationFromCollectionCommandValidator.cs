using FluentValidation;

namespace Vivu.Application.UseCases.Collections.Commands.RemoveLocationFromCollection
{
    public class RemoveLocationFromCollectionCommandValidator : AbstractValidator<RemoveLocationFromCollectionCommand>
    {
        public RemoveLocationFromCollectionCommandValidator()
        {
            RuleFor(x => x.CollectionId).NotEmpty().WithMessage("CollectionId is required.");
            RuleFor(x => x.LocationId).NotEmpty().WithMessage("LocationId is required.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        }
    }
}
