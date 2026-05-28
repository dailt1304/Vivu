using MediatR;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Users;
using UserRoleEnum = Vivu.Domain.Enums.UserRole;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.CreateUser
{
    public class CreateUserCommand : IRequest<Result<UserDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRoleEnum Role { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public Guid? CountryId { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
    }
}

