using FluentValidation;

namespace Vivu.Application.UseCases.Users.Commands.BanUser
{
    public class BanUserCommandValidator : AbstractValidator<BanUserCommand>
    {
        public BanUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
