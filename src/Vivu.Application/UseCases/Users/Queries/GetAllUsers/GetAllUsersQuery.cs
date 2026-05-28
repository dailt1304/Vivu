using MediatR;
using Vivu.Application.Common;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.GetAllUsers
{
    public class GetAllUsersQuery : PaginationRequest, IRequest<Result<PaginatedList<UserDto>>>
    {
    }
}

