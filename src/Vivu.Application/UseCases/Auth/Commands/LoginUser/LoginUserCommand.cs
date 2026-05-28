using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.LoginUser
{
    public class LoginUserCommand : IRequest<Result<LoginResponse>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }
}
