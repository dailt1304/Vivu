using FluentValidation;

namespace Vivu.Application.UseCases.Payments.Commands.CancelPayment;

public class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("Transaction ID is required.");
    }
}
