using MediatR;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.BanUser
{
    public class BanUserCommand : IRequest<Result<UserDto>>
    {
        public Guid UserId { get; set; }
        public string? Reason { get; set; }
    }
}
