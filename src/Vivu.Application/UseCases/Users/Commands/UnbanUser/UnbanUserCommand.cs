using MediatR;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.UnbanUser
{
    public class UnbanUserCommand : IRequest<Result<UserDto>>
    {
        public Guid UserId { get; set; }
    }
}
