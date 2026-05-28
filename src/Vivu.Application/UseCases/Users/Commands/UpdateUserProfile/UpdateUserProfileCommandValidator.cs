using FluentValidation;

namespace Vivu.Application.UseCases.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.FullName)
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.FullName));

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio must not exceed 500 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.Date)
                .WithMessage("Date of birth must be in the past")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Gender)
                .MaximumLength(20).WithMessage("Gender must not exceed 20 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Gender));

            RuleFor(x => x.AvatarUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Avatar URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        }
    }
}
