using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.SearchUsers
{
    public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, Result<PaginatedList<UserDto>>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchUsersQueryHandler> _logger;

        public SearchUsersQueryHandler(
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<SearchUsersQueryHandler> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<UserDto>>> Handle(
            SearchUsersQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Searching users - SearchTerm: {SearchTerm}, Status: {Status}, Role: {Role}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SearchTerm,
                request.Status,
                request.Role,
                request.PageNumber,
                request.PageSize);

            var query = _userRepository.GetAllQuery()
                .Include(u => u.UserProfile)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower().Trim();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.Phone != null && u.Phone.Contains(searchTerm)) ||
                    (u.UserProfile != null && u.UserProfile.FullName != null &&
                     u.UserProfile.FullName.ToLower().Contains(searchTerm)));

                _logger.LogDebug("Applied search term filter: {SearchTerm}", request.SearchTerm);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(u => u.Status == request.Status);
                _logger.LogDebug("Applied status filter: {Status}", request.Status);
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var roleName = request.Role.ToUpper();
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == roleName));
                _logger.LogDebug("Applied role filter: {Role}", request.Role);
            }

            if (request.IsEmailVerified.HasValue)
            {
                query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);
                _logger.LogDebug("Applied email verified filter: {IsEmailVerified}", request.IsEmailVerified.Value);
            }

            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(u => u.CreatedDate >= request.CreatedFrom.Value);
                _logger.LogDebug("Applied CreatedFrom filter: {CreatedFrom}", request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                query = query.Where(u => u.CreatedDate <= request.CreatedTo.Value);
                _logger.LogDebug("Applied CreatedTo filter: {CreatedTo}", request.CreatedTo.Value);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortColumn))
            {
                var sortColumn = request.SortColumn.ToLower();
                query = sortColumn switch
                {
                    "email" => request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                    "fullname" => request.SortDescending ? query.OrderByDescending(u => u.UserProfile != null ? u.UserProfile.FullName : null) : query.OrderBy(u => u.UserProfile != null ? u.UserProfile.FullName : null),
                    "status" => request.SortDescending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
                    "createdat" or "createddate" => request.SortDescending ? query.OrderByDescending(u => u.CreatedDate) : query.OrderBy(u => u.CreatedDate),
                    "lastloginat" => request.SortDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                    _ => query.OrderByDescending(u => u.CreatedDate)
                };
                _logger.LogDebug("Applied sorting: {SortColumn} {Direction}", request.SortColumn, request.SortDescending ? "DESC" : "ASC");
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedDate);
            }

            var paginatedUsers = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var paginatedResult = paginatedUsers.Map(u => _mapper.Map<UserDto>(u));

            _logger.LogInformation(
                "Found {TotalCount} users matching search criteria, returning page {PageNumber} with {Count} items",
                paginatedResult.TotalCount,
                paginatedResult.PageNumber,
                paginatedResult.Items.Count);

            return Result<PaginatedList<UserDto>>.Success(paginatedResult);
        }
    }
}
