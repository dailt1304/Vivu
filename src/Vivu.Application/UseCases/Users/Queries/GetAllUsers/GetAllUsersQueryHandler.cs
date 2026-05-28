using AutoMapper;
using MediatR;
using Vivu.Application.Common;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<PaginatedList<UserDto>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetAllUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _userRepository
                .GetAllQuery()
                .OrderByDescending(u => u.CreatedDate);

            var paginatedUsers = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var paginatedResult = paginatedUsers.Map(u => _mapper.Map<UserDto>(u));

            return Result<PaginatedList<UserDto>>.Success(paginatedResult);
        }
    }
}
