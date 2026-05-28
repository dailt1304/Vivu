using FluentValidation;

namespace Vivu.Application.UseCases.Payments.Commands.CreatePaymentLink;

public class CreatePaymentLinkCommandValidator : AbstractValidator<CreatePaymentLinkCommand>
{
    public CreatePaymentLinkCommandValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required.");
    }
}
