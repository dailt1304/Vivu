using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Notifications.Events;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.CreateComment
{
    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, Result<BlogCommentDto>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;
        private readonly IPublisher _publisher;
        private readonly ILogger<CreateCommentCommandHandler> _logger;

        public CreateCommentCommandHandler(
            IBlogRepository blogRepository,
            IBlogCommentRepository commentRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IUserRepository userRepository,
            IPublisher publisher,
            ILogger<CreateCommentCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userRepository = userRepository;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<BlogCommentDto>> Handle(
            CreateCommentCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<BlogCommentDto>.Failure(DomainErrors.Auth.InvalidToken);

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
                return Result<BlogCommentDto>.Failure(DomainErrors.Blog.NotFound);

            if (blog.Status == "deleted")
                return Result<BlogCommentDto>.Failure(DomainErrors.Blog.HasDeleted);

            if (blog.Status != "published")
                return Result<BlogCommentDto>.Failure(DomainErrors.Blog.NotPublished);

            var comment = BlogComment.Create(request.BlogId, userId, request.Content);

            await _commentRepository.AddAsync(comment);

            // Increment blog comment count
            blog.CommentCount++;
            blog.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} created comment {CommentId} on blog {BlogId}",
                userId, comment.Id, request.BlogId);

            // Notify blog owner (skip if commenter is the owner)
            if (blog.UserId != userId)
            {
                var commenter = await _userRepository.GetByIdAsync(userId);
                await _publisher.Publish(new NewCommentEvent(
                    blogOwnerId: blog.UserId,
                    blogId: blog.Id,
                    commenterName: commenter?.UserProfile?.FullName ?? "Người dùng",
                    blogTitle: blog.Title ?? "bài viết"), cancellationToken);
            }

            return Result<BlogCommentDto>.Success(new BlogCommentDto
            {
                Id = comment.Id,
                BlogId = comment.BlogId,
                UserId = comment.UserId,
                Content = comment.Content,
                LikeCount = comment.LikeCount,
                CreatedAt = comment.CreatedDate
            });
        }
    }
}
