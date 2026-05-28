using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.DeleteComment
{
    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogCommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DeleteCommentCommandHandler> _logger;

        public DeleteCommentCommandHandler(
            IBlogRepository blogRepository,
            IBlogCommentRepository commentRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<DeleteCommentCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteCommentCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);

            var comment = await _commentRepository.GetByIdAsync(request.CommentId);
            if (comment == null)
                return Result<bool>.Failure(DomainErrors.BlogComment.NotFound);

            // only owner of comment or blog can delete comment
            if (comment.UserId != userId)
            {
                var blog = await _blogRepository.GetByIdAsync(comment.BlogId);
                if (blog == null || blog.UserId != userId)
                    return Result<bool>.Failure(DomainErrors.BlogComment.AccessDenied);
            }

            _commentRepository.Remove(comment);

            // Decrement blog comment count
            var blogToUpdate = await _blogRepository.GetByIdAsync(comment.BlogId);
            if (blogToUpdate != null)
            {
                blogToUpdate.CommentCount = Math.Max(0, blogToUpdate.CommentCount - 1);
                blogToUpdate.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Comment {CommentId} deleted by user {UserId}", request.CommentId, userId);

            return Result<bool>.Success(true);
        }
    }
}
