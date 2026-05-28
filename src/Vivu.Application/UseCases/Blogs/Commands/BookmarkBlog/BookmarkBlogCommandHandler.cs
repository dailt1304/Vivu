using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog
{
    public class BookmarkBlogCommandHandler : IRequestHandler<BookmarkBlogCommand, Result<BookmarkBlogResponse>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogSaveRepository _blogSaveRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<BookmarkBlogCommandHandler> _logger;

        public BookmarkBlogCommandHandler(
            IBlogRepository blogRepository,
            IBlogSaveRepository blogSaveRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<BookmarkBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _blogSaveRepository = blogSaveRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<BookmarkBlogResponse>> Handle(
            BookmarkBlogCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("BookmarkBlog failed: Invalid or missing user token");
                return Result<BookmarkBlogResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);
            if (blog == null)
            {
                _logger.LogWarning("BookmarkBlog failed: Blog {BlogId} not found", request.BlogId);
                return Result<BookmarkBlogResponse>.Failure(DomainErrors.Blog.NotFound);
            }

            if (blog.Status == "deleted")
            {
                _logger.LogWarning("BookmarkBlog failed: Blog {BlogId} has been deleted", request.BlogId);
                return Result<BookmarkBlogResponse>.Failure(DomainErrors.Blog.HasDeleted);
            }

            if (blog.Status != "published")
            {
                _logger.LogWarning("BookmarkBlog failed: Blog {BlogId} is not published", request.BlogId);
                return Result<BookmarkBlogResponse>.Failure(DomainErrors.Blog.NotPublished);
            }

            var existingSave = await _blogSaveRepository.GetBlogSaveAsync(request.BlogId, userId, cancellationToken);
            bool isBookmarked;

            if (existingSave == null)
            {
                // Toggle ON – add bookmark
                var save = BlogSave.Create(request.BlogId, userId);
                await _blogSaveRepository.AddAsync(save);
                blog.SaveCount++;
                blog.UpdatedAt = DateTime.UtcNow;
                isBookmarked = true;
                _logger.LogDebug("User {UserId} bookmarked blog {BlogId}", userId, request.BlogId);
            }
            else
            {
                // Toggle OFF – remove bookmark
                _blogSaveRepository.Remove(existingSave);
                blog.SaveCount = Math.Max(0, blog.SaveCount - 1);
                blog.UpdatedAt = DateTime.UtcNow;
                isBookmarked = false;
                _logger.LogDebug("User {UserId} un-bookmarked blog {BlogId}", userId, request.BlogId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<BookmarkBlogResponse>.Success(new BookmarkBlogResponse
            {
                IsBookmarked = isBookmarked,
                SaveCount = blog.SaveCount
            });
        }
    }
}
