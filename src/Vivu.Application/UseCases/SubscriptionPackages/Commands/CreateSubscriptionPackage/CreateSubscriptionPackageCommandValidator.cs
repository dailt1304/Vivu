using FluentValidation;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.CreateSubscriptionPackage;

public class CreateSubscriptionPackageCommandValidator : AbstractValidator<CreateSubscriptionPackageCommand>
{
    public CreateSubscriptionPackageCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Code must contain only uppercase letters, numbers, underscores, and hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duration days must be greater than 0.")
            .LessThanOrEqualTo(3650).WithMessage("Duration days must not exceed 10 years (3650 days).");

        RuleFor(x => x.MaxAiRequestPerDay)
            .GreaterThanOrEqualTo(0).WithMessage("Max AI requests per day must be greater than or equal to 0.")
            .LessThanOrEqualTo(10000).WithMessage("Max AI requests per day must not exceed 10,000.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid subscription type (FREE, PREMIUM, or PRO).")
            .When(x => x.Type.HasValue);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0.");
    }
}
