using FluentValidation;
using Vivu.Domain.Entities;
using UserRoleEnum = Vivu.Domain.Enums.UserRole;

namespace Vivu.Application.UseCases.Users.Commands.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(100);

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.Date)
                .WithMessage("Date of birth must be in the past")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Gender)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.Gender));

            RuleFor(x => x.AvatarUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("AvatarUrl must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone number")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Role is invalid")
                .Must(r => r == UserRoleEnum.Admin || r == UserRoleEnum.Moderator)
                .WithMessage($"Role must be {UserRoleEnum.Admin} or {UserRoleEnum.Moderator}");
        }
    }
}

