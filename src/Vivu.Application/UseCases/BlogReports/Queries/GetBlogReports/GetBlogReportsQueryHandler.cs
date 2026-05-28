using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.BlogReports.Queries.GetBlogReports;

public class GetBlogReportsQueryHandler : IRequestHandler<GetBlogReportsQuery, Result<PaginatedList<BlogReportDto>>>
{
    private readonly IBlogReportRepository _blogReportRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetBlogReportsQueryHandler> _logger;

    public GetBlogReportsQueryHandler(
        IBlogReportRepository blogReportRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        ILogger<GetBlogReportsQueryHandler> logger)
    {
        _blogReportRepository = blogReportRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<BlogReportDto>>> Handle(GetBlogReportsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Get blog reports attempt. Status: {Status}, ReportType: {ReportType}, Page: {PageNumber}, PageSize: {PageSize}",
            request.Status ?? "all",
            request.ReportType ?? "all",
            request.PageNumber,
            request.PageSize);

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Get blog reports failed: Invalid or missing user ID");
            return Result<PaginatedList<BlogReportDto>>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var query = _blogReportRepository.GetBlogReportsQuery(request.Status, request.ReportType);

        var paginatedReports = await query.ToPaginatedListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var reportDtos = _mapper.Map<List<BlogReportDto>>(paginatedReports.Items);

        var result = new PaginatedList<BlogReportDto>(
            reportDtos,
            paginatedReports.TotalCount,
            paginatedReports.PageNumber,
            paginatedReports.PageSize);

        _logger.LogInformation(
            "Blog reports retrieved successfully. TotalCount: {TotalCount}, PageNumber: {PageNumber}",
            result.TotalCount,
            result.PageNumber);

        return Result<PaginatedList<BlogReportDto>>.Success(result);
    }
}
