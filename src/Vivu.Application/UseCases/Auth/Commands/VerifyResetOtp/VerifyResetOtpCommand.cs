using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp
{
    public record VerifyResetOtpCommand(
    string Email,
    string Code) : IRequest<Result>;
}
