using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Result<RefreshTokenResponse>>
    {
        public string RefreshToken { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
    }
}
