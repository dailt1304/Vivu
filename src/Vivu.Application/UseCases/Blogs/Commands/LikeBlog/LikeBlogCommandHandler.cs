using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.LikeBlog
{
    public class LikeBlogCommandHandler : IRequestHandler<LikeBlogCommand, Result<LikeBlogResponse>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogLikeRepository _blogLikeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly IPublisher _publisher;
        private readonly ILogger<LikeBlogCommandHandler> _logger;

        public LikeBlogCommandHandler(
            IBlogRepository blogRepository,
            IBlogLikeRepository blogLikeRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IUserRepository userRepository,
            IPublisher publisher,
            ILogger<LikeBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _blogLikeRepository = blogLikeRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userRepository = userRepository;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<LikeBlogResponse>> Handle(
            LikeBlogCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("LikeBlog failed: Invalid or missing user token");
                return Result<LikeBlogResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
            {
                _logger.LogWarning("LikeBlog failed: Blog {BlogId} not found", request.BlogId);
                return Result<LikeBlogResponse>.Failure(DomainErrors.Blog.NotFound);
            }

            if (blog.Status == "deleted")
            {
                _logger.LogWarning("LikeBlog failed: Blog {BlogId} has been deleted", request.BlogId);
                return Result<LikeBlogResponse>.Failure(DomainErrors.Blog.HasDeleted);
            }

            if (blog.Status != "published")
            {
                _logger.LogWarning("LikeBlog failed: Blog {BlogId} is not published", request.BlogId);
                return Result<LikeBlogResponse>.Failure(DomainErrors.Blog.NotPublished);
            }

            var existingLike = await _blogLikeRepository.GetBlogLikeAsync(request.BlogId, userId, cancellationToken);
            bool isLiked;

            if (existingLike == null)
            {
                // Toggle ON add like
                var like = BlogLike.Create(request.BlogId, userId);
                await _blogLikeRepository.AddAsync(like);
                blog.LikeCount++;
                blog.UpdatedAt = DateTime.UtcNow;
                isLiked = true;
                _logger.LogDebug("User {UserId} liked blog {BlogId}", userId, request.BlogId);
            }
            else
            {
                // Toggle OFF remove like
                _blogLikeRepository.Remove(existingLike);
                blog.LikeCount = Math.Max(0, blog.LikeCount - 1);
                blog.UpdatedAt = DateTime.UtcNow;
                isLiked = false;
                _logger.LogDebug("User {UserId} unliked blog {BlogId}", userId, request.BlogId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify blog owner on like (not unlike), skip self-like
            if (isLiked && blog.UserId != userId)
            {
                var liker = await _userRepository.GetByIdAsync(userId);
                await _publisher.Publish(new NewLikeEvent(
                    blogOwnerId: blog.UserId,
                    blogId: blog.Id,
                    likerName: liker?.UserProfile?.FullName ?? "Người dùng",
                    blogTitle: blog.Title ?? "bài viết"), cancellationToken);
            }

            return Result<LikeBlogResponse>.Success(new LikeBlogResponse
            {
                IsLiked = isLiked,
                LikeCount = blog.LikeCount
            });
        }
    }
}

