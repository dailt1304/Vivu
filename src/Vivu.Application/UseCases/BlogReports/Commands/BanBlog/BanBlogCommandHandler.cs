    using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Commands.BanBlog;

public class BanBlogCommandHandler : IRequestHandler<BanBlogCommand, Result<bool>>
{
    private readonly IBlogRepository _blogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<BanBlogCommandHandler> _logger;

    public BanBlogCommandHandler(
        IBlogRepository blogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<BanBlogCommandHandler> logger)
    {
        _blogRepository = blogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(BanBlogCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var moderatorId))
        {
            _logger.LogWarning("Ban blog failed: Invalid or missing user ID");
            return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var blog = await _blogRepository.GetByIdAsync(request.BlogId);
        if (blog == null)
        {
            _logger.LogWarning("Ban blog failed: Blog {BlogId} not found", request.BlogId);
            return Result<bool>.Failure(DomainErrors.Blog.NotFound);
        }

        if (blog.Status == "banned")
        {
            _logger.LogWarning("Ban blog skipped: Blog {BlogId} is already banned", request.BlogId);
            return Result<bool>.Failure(DomainErrors.Blog.AlreadyBanned);
        }

        blog.Status = "banned";
        blog.UpdatedAt = DateTime.UtcNow;

        _blogRepository.Update(blog);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Blog {BlogId} has been banned by moderator {ModeratorId}",
            request.BlogId,
            moderatorId);

        return Result<bool>.Success(true);
    }
}
