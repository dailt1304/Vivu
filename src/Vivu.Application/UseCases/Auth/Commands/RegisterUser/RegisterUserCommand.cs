using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Auth.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Result<UserDto>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string OtpCode { get; set; } = string.Empty;
    }
}
