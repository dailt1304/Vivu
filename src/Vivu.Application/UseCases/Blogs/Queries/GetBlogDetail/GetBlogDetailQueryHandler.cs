using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQueryHandler : IRequestHandler<GetBlogDetailQuery, Result<BlogDetailDto>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogLikeRepository _blogLikeRepository;
        private readonly IBlogSaveRepository _blogSaveRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<GetBlogDetailQueryHandler> _logger;

        public GetBlogDetailQueryHandler(
            IBlogRepository blogRepository,
            IBlogLikeRepository blogLikeRepository,
            IBlogSaveRepository blogSaveRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<GetBlogDetailQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _blogLikeRepository = blogLikeRepository;
            _blogSaveRepository = blogSaveRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<BlogDetailDto>> Handle(
            GetBlogDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching blog detail for IdOrSlug: {IdOrSlug}",
                request.IdOrSlug);

            var blog = await _blogRepository.GetBlogDetailByIdOrSlugAsync(
                request.IdOrSlug, cancellationToken);

            if (blog == null)
            {
                _logger.LogWarning("Blog not found for IdOrSlug: {IdOrSlug}", request.IdOrSlug);
                return Result<BlogDetailDto>.Failure(DomainErrors.Blog.NotFound);
            }

            if (blog.Status != "published")
            {
                var hasPermission = !string.IsNullOrEmpty(_currentUser.Id) 
                                 && Guid.TryParse(_currentUser.Id, out var parsedUserId) 
                                 && parsedUserId == blog.UserId;

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "Blog {BlogId} is not published (Status: {Status}) and requester is not the author",
                        blog.Id, blog.Status);
                    return Result<BlogDetailDto>.Failure(DomainErrors.Blog.NotPublished);
                }
            }

            // Track view count only if published
            if (blog.Status == "published")
            {
                blog.ViewCount++;
                _blogRepository.Update(blog);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogDebug(
                    "View count incremented for blog {BlogId}, new count: {ViewCount}",
                    blog.Id, blog.ViewCount);
            }

            var dto = _mapper.Map<BlogDetailDto>(blog);

            // Enrich with current user interaction state
            if (!string.IsNullOrEmpty(_currentUser.Id) && Guid.TryParse(_currentUser.Id, out var userId))
            {
                var existingLike = await _blogLikeRepository.GetBlogLikeAsync(blog.Id, userId, cancellationToken);
                dto.IsLikedByCurrentUser = existingLike != null;

                var existingSave = await _blogSaveRepository.GetBlogSaveAsync(blog.Id, userId, cancellationToken);
                dto.IsBookmarkedByCurrentUser = existingSave != null;
            }

            _logger.LogInformation(
                "Successfully fetched blog detail {BlogId} - '{Title}'",
                blog.Id, blog.Title);

            return Result<BlogDetailDto>.Success(dto);
        }
    }
}
