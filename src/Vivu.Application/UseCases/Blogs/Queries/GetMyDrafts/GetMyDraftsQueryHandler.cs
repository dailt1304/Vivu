using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.GetMyDrafts
{
    public class GetMyDraftsQueryHandler : IRequestHandler<GetMyDraftsQuery, Result<List<PublicBlogDto>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<GetMyDraftsQueryHandler> _logger;

        public GetMyDraftsQueryHandler(
            IBlogRepository blogRepository,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<GetMyDraftsQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<PublicBlogDto>>> Handle(
            GetMyDraftsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("GetMyDrafts failed: Invalid or missing user token");
                return Result<List<PublicBlogDto>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Fetching drafts for UserId: {UserId}", userId);

            var query = _blogRepository.GetBlogsByUserIdQuery(userId);
            
            // Only fetch drafts
            query = query.Where(b => b.Status == "draft");
            
            var drafts = await query
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully fetched {Count} drafts for user {UserId}",
                drafts.Count,
                userId);

            var blogDtos = drafts
                .Select(blog => _mapper.Map<PublicBlogDto>(blog))
                .ToList();

            return Result<List<PublicBlogDto>>.Success(blogDtos);
        }
    }
}
