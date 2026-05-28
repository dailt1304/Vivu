using MediatR;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommand : IRequest<Result<UserDto>>
    {
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public Guid? CountryId { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Phone { get; set; }
    }
}
