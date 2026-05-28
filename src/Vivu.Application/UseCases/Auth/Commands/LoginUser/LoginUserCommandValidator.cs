using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Auth.Commands.LoginUser
{
    public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        private const string StrictEmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        public LoginUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .Matches(StrictEmailPattern).WithMessage("Invalid email format")
                .Must(email =>
                {
                    if (string.IsNullOrEmpty(email)) return true;

                    if (email.Contains(" ")) return false;

                    if (email.Contains("..")) return false;

                    if (email.Count(c => c == '@') != 1) return false;

                    if (email.Any(c => c > 127)) return false;

                    return true;
                })
                .WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }

    }
}
