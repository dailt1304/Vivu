using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp
{
    public class VerifyResetOtpCommandValidator : AbstractValidator<VerifyResetOtpCommand>
    {
        public VerifyResetOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("OTP code is required.")
                .Length(6).WithMessage("OTP code must be 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits.");
        }
    }
}
