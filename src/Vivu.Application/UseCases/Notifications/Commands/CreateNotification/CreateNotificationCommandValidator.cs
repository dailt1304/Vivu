using FluentValidation;

namespace Vivu.Application.UseCases.Notifications.Commands.CreateNotification
{
    public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Notification Type is required.");
        }
    }
}
