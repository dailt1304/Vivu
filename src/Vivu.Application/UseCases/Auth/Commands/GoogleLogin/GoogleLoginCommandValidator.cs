using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Vivu.Application.UseCases.GoogleLogin
{
    public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
    {
        public GoogleLoginCommandValidator()
        {
            RuleFor(x => x.IdToken)
                .NotEmpty()
                .WithMessage("Google ID token is required");
        }
    }
}
