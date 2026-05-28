using FluentValidation;

namespace Vivu.Application.UseCases.Notifications.Queries.GetUserNotifications
{
    public class GetUserNotificationsQueryValidator : AbstractValidator<GetUserNotificationsQuery>
    {
        public GetUserNotificationsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");
        }
    }
}
