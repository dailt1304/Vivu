using FluentValidation;

namespace Vivu.Application.UseCases.Users.Commands.UnbanUser
{
    public class UnbanUserCommandValidator : AbstractValidator<UnbanUserCommand>
    {
        public UnbanUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}
