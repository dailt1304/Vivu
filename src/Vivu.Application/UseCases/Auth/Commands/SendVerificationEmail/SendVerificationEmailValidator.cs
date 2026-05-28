using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;

namespace Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailValidator: AbstractValidator<SendVerificationEmailCommand>
    {
        private const string StrictEmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        public SendVerificationEmailValidator()
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
                .WithMessage("Invalid email format")
                .Must(email =>
                {
                    if (string.IsNullOrEmpty(email)) return true;
                    if (email.Length >= 254) return false;
                    return true;
                })
                .WithMessage("Email must not exceed 254 characters")
                .Must(email =>
                {
                    if (string.IsNullOrEmpty(email)) return true;

                    var parts = email.Split('@');
                    if (parts.Length != 2) return false;

                    var localPart = parts[0];
                    var domainPart = parts[1];

                    if (string.IsNullOrEmpty(localPart)) return false;
                    if (localPart.Contains("..")) return false;
                    if (localPart.StartsWith(".") || localPart.EndsWith(".")) return false; 

                    if (string.IsNullOrEmpty(domainPart)) return false;

                    if (!domainPart.Contains(".")) return false;

                    if (domainPart.StartsWith(".") || domainPart.EndsWith(".")) return false;

                    if (domainPart.Contains("..")) return false;

                    var domainParts = domainPart.Split('.');
                    var tld = domainParts.Last();

                    if (tld.Length < 2) return false;

                    return true;
                })
                .WithMessage("Invalid email format");

        }
    }
}
