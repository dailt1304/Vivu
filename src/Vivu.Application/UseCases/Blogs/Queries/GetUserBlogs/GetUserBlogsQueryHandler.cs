using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs
{
    public class GetUserBlogsQueryHandler : IRequestHandler<GetUserBlogsQuery, Result<PaginatedList<PublicBlogDto>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserBlogsQueryHandler> _logger;

        public GetUserBlogsQueryHandler(
            IBlogRepository blogRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<GetUserBlogsQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicBlogDto>>> Handle(
            GetUserBlogsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching blogs for UserId: {UserId}, Page: {PageNumber}, PageSize: {PageSize}",
                request.UserId,
                request.PageNumber,
                request.PageSize);

            // Verify user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", request.UserId);
                return Result<PaginatedList<PublicBlogDto>>.Failure(DomainErrors.User.NotFoundById(request.UserId));
            }

            var query = _blogRepository.GetBlogsByUserIdQuery(request.UserId);

            if (!await query.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("No blogs found for user {UserId}", request.UserId);

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

            _logger.LogDebug(
                "Retrieved {Count} blogs out of {TotalCount} for user {UserId}",
                paginatedBlogs.Items.Count,
                paginatedBlogs.TotalCount,
                request.UserId);

            var blogDtos = paginatedBlogs.Items
                .Select(blog => _mapper.Map<PublicBlogDto>(blog))
                .ToList();

            var result = new PaginatedList<PublicBlogDto>(
                blogDtos,
                paginatedBlogs.TotalCount,
                paginatedBlogs.PageNumber,
                paginatedBlogs.PageSize);

            _logger.LogInformation(
                "Successfully fetched {Count} blogs for user {UserId}",
                result.Items.Count,
                request.UserId);

            return Result<PaginatedList<PublicBlogDto>>.Success(result);
        }
    }
}
