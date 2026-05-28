using FluentValidation;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.SubscribeSubscriptionPackage;

public class SubscribeSubscriptionPackageCommandValidator : AbstractValidator<SubscribeSubscriptionPackageCommand>
{
    public SubscribeSubscriptionPackageCommandValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("PackageId is required.");
    }
}