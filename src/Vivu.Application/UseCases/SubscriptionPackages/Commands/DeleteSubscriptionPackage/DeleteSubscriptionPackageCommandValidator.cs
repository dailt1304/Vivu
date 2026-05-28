using FluentValidation;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.DeleteSubscriptionPackage;

public class DeleteSubscriptionPackageCommandValidator : AbstractValidator<DeleteSubscriptionPackageCommand>
{
    public DeleteSubscriptionPackageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Package ID is required.");
    }
}
