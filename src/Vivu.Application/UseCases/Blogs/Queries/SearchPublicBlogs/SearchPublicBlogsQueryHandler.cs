using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Queries.SearchPublicBlogs
{
    public class SearchPublicBlogsQueryHandler : IRequestHandler<SearchPublicBlogsQuery, Result<PaginatedList<PublicBlogDto>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ISearchTermTrip _searchTermService;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchPublicBlogsQueryHandler> _logger;

        public SearchPublicBlogsQueryHandler(
            IBlogRepository blogRepository,
            ISearchTermTrip searchTermService,
            IMapper mapper,
            ILogger<SearchPublicBlogsQueryHandler> logger)
        {
            _blogRepository = blogRepository;
            _searchTermService = searchTermService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<PublicBlogDto>>> Handle(
            SearchPublicBlogsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Searching public blogs - SearchTerm: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
                request.SearchTerm,
                request.PageNumber,
                request.PageSize);

            // Sanitize search term
            var searchTerm = _searchTermService.SanitizeSearchTerm(request.SearchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("Search term after sanitization is null or empty: {SearchTerm}", searchTerm);
                return Result<PaginatedList<PublicBlogDto>>.Failure(
                    DomainErrors.Blog.InvalidSearchTerm);
            }

            _logger.LogDebug("Search term sanitized: {SanitizedTerm}", searchTerm);

            // Convert to tsquery
            var tsQuery = _searchTermService.ConvertToTsQuery(searchTerm);
            _logger.LogDebug("Converted to tsquery: {TsQuery}", tsQuery);

            // Get blogs matching search term
            var query = _blogRepository.GetSearchTermBlogs(tsQuery);
            _logger.LogDebug("Found {Count} blogs after applying search term", query.Count());

            // Apply sorting by relevance (search rank)
            query = query.OrderByDescending(b =>
                EF.Property<NpgsqlTypes.NpgsqlTsVector>(b, "SearchVector")
                    .Rank(EF.Functions.ToTsQuery("simple", tsQuery)));

            _logger.LogDebug("Applied relevance sorting");

            // Paginate
            var paginatedBlogs = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} blogs out of {TotalCount}",
                paginatedBlogs.Items.Count,
                paginatedBlogs.TotalCount);

            // Map to DTOs
            var blogDtos = paginatedBlogs.Items
                .Select(blog => _mapper.Map<PublicBlogDto>(blog))
                .ToList();

            var result = new PaginatedList<PublicBlogDto>(
                blogDtos,
                paginatedBlogs.TotalCount,
                paginatedBlogs.PageNumber,
                paginatedBlogs.PageSize);

            _logger.LogInformation(
                "Successfully searched {Count} public blogs",
                result.Items.Count);

            return Result<PaginatedList<PublicBlogDto>>.Success(result);
        }
    }
}
