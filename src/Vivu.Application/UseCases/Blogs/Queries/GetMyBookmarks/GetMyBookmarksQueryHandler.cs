using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetMyBookmarks
{
    public class GetMyBookmarksQueryHandler : IRequestHandler<GetMyBookmarksQuery, Result<PaginatedList<PublicBlogDto>>>
    {
        private readonly IBlogSaveRepository _blogSaveRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<GetMyBookmarksQueryHandler> _logger;

        public GetMyBookmarksQueryHandler(
            IBlogSaveRepository blogSaveRepository,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<GetMyBookmarksQueryHandler> logger)
        {
            _blogSaveRepository = blogSaveRepository;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicBlogDto>>> Handle(
            GetMyBookmarksQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("GetMyBookmarks failed: Invalid or missing user token");
                return Result<PaginatedList<PublicBlogDto>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Fetching bookmarks for UserId: {UserId}, Page: {PageNumber}, PageSize: {PageSize}",
                userId, request.PageNumber, request.PageSize);

            var query = _blogSaveRepository.GetBookmarkedBlogsByUserIdQuery(userId);

            if (!await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No bookmarks found for user {UserId}", userId);
                var emptyResult = new PaginatedList<PublicBlogDto>(
                    new List<PublicBlogDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                return Result<PaginatedList<PublicBlogDto>>.Success(emptyResult);
            }

            query = query.OrderByDescending(b => b.PublishedAt);

            var paginatedBlogs = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var blogDtos = paginatedBlogs.Items
                .Select(blog => _mapper.Map<PublicBlogDto>(blog))
                .ToList();

            var result = new PaginatedList<PublicBlogDto>(
                blogDtos,
                paginatedBlogs.TotalCount,
                paginatedBlogs.PageNumber,
                paginatedBlogs.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} bookmarks for user {UserId}",
                result.Items.Count, userId);

            return Result<PaginatedList<PublicBlogDto>>.Success(result);
        }
    }
}
