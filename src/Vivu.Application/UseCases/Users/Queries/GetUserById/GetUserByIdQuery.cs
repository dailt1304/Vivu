using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<Result<UserDto>>
    {
        public Guid UserId { get; set; }
    }
}
