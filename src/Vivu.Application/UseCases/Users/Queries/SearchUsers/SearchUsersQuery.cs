using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.SearchUsers
{
    public class SearchUsersQuery : PaginationRequest, IRequest<Result<PaginatedList<UserDto>>>
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Role { get; set; }
        public bool? IsEmailVerified { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
