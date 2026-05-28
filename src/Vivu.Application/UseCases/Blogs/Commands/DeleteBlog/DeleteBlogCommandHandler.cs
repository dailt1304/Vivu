using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.DeleteBlog
{
    public class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, Result<bool>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DeleteBlogCommandHandler> _logger;

        public DeleteBlogCommandHandler(
            IBlogRepository blogRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<DeleteBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteBlogCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);

            var blog = await _blogRepository.GetByIdAsync(request.BlogId);

            if (blog == null)
            {
                return Result<bool>.Failure(DomainErrors.Blog.NotFound);
            }

            if (blog.UserId != userId)
            {
                return Result<bool>.Failure(DomainErrors.Blog.NotOwner);
            }

            blog.Status = "deleted";
            blog.UpdatedAt = DateTime.UtcNow;

            _blogRepository.Update(blog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Blog {BlogId} soft deleted by user {UserId}", request.BlogId, userId);

            return Result<bool>.Success(true);
        }
    }
}
