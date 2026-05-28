using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail
{
    public record SendVerificationEmailCommand(string Email) : IRequest<Result>;

}
