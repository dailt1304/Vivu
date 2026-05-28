using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogs
{
    public class GetBlogsQueryHandler : IRequestHandler<GetBlogsQuery, Result<PaginatedList<PublicBlogDto>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogLikeRepository _blogLikeRepository;
        private readonly IBlogSaveRepository _blogSaveRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<GetBlogsQueryHandler> _logger;

        public GetBlogsQueryHandler(
            IBlogRepository blogRepository,
            IBlogLikeRepository blogLikeRepository,
            IBlogSaveRepository blogSaveRepository,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<GetBlogsQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _blogLikeRepository = blogLikeRepository;
            _blogSaveRepository = blogSaveRepository;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicBlogDto>>> Handle(
            GetBlogsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching public blogs - SortBy: {SortBy}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SortBy,
                request.PageNumber,
                request.PageSize);

            var query = _blogRepository.GetPublicBlogsQuery(request.IsAdmin, request.Status);

            if (!await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No public blogs found");

                var emptyResult = new PaginatedList<PublicBlogDto>(
                    new List<PublicBlogDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);

                return Result<PaginatedList<PublicBlogDto>>.Success(emptyResult);
            }

            // Apply sorting
            var sortBy = (request.SortBy ?? "newest").ToLower();
            query = sortBy switch
            {
                "most_viewed"  => query.OrderByDescending(b => b.ViewCount),
                "most_liked"   => query.OrderByDescending(b => b.LikeCount),
                "popular"      => query.OrderByDescending(b => b.LikeCount + b.ViewCount + b.SaveCount),
                _              => query.OrderByDescending(b => b.PublishedAt)   // newest
            };

            _logger.LogDebug("Applied sorting: {SortBy}", sortBy);

            var paginatedBlogs = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} blogs out of {TotalCount}",
                paginatedBlogs.Items.Count,
                paginatedBlogs.TotalCount);

            var blogDtos = paginatedBlogs.Items
                .Select(blog => _mapper.Map<PublicBlogDto>(blog))
                .ToList();

            // Enrich with current user interaction state
            if (!string.IsNullOrEmpty(_currentUser.Id) && Guid.TryParse(_currentUser.Id, out var userId))
            {
                var blogIds = blogDtos.Select(b => b.Id).ToList();
                var likedIds = await _blogLikeRepository.GetLikedBlogIdsAsync(userId, blogIds, cancellationToken);
                var savedIds = await _blogSaveRepository.GetSavedBlogIdsAsync(userId, blogIds, cancellationToken);

                foreach (var dto in blogDtos)
                {
                    dto.IsLikedByCurrentUser = likedIds.Contains(dto.Id);
                    dto.IsBookmarkedByCurrentUser = savedIds.Contains(dto.Id);
                }
            }

            var result = new PaginatedList<PublicBlogDto>(
                blogDtos,
                paginatedBlogs.TotalCount,
                paginatedBlogs.PageNumber,
                paginatedBlogs.PageSize);

            _logger.LogInformation("Successfully fetched {Count} public blogs", result.Items.Count);

            return Result<PaginatedList<PublicBlogDto>>.Success(result);
        }
    }
}
