using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetComments
{
    public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, Result<PaginatedList<BlogCommentDto>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCommentRepository _commentRepository;
        private readonly ILogger<GetCommentsQueryHandler> _logger;

        public GetCommentsQueryHandler(
            IBlogRepository blogRepository,
            IBlogCommentRepository commentRepository,
            ILogger<GetCommentsQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _commentRepository = commentRepository;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<BlogCommentDto>>> Handle(
            GetCommentsQuery request,
            CancellationToken cancellationToken)
        {
            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
                return Result<PaginatedList<BlogCommentDto>>.Failure(DomainErrors.Blog.NotFound);

            var query = _commentRepository.GetCommentsByBlogIdQuery(request.BlogId)
                .Where(c => !c.IsHidden)
                .OrderByDescending(c => c.CreatedDate);

            var paginated = await query.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);

            var dtos = paginated.Items.Select(c => new BlogCommentDto
            {
                Id = c.Id,
                BlogId = c.BlogId,
                UserId = c.UserId,
                Content = c.Content,
                LikeCount = c.LikeCount,
                CreatedAt = c.CreatedDate,
                AuthorName = c.User?.UserProfile?.FullName ?? c.User?.Email,
                AuthorAvatarUrl = c.User?.UserProfile?.AvatarUrl
            }).ToList();

            _logger.LogInformation("Fetched {Count} comments for blog {BlogId}", dtos.Count, request.BlogId);

            return Result<PaginatedList<BlogCommentDto>>.Success(
                new PaginatedList<BlogCommentDto>(dtos, paginated.TotalCount, paginated.PageNumber, paginated.PageSize));
        }
    }
}
