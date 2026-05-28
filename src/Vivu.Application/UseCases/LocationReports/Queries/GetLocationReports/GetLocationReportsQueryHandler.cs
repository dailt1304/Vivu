using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Queries.GetLocationReports;

public class GetLocationReportsQueryHandler : IRequestHandler<GetLocationReportsQuery, Result<PaginatedList<LocationReportDto>>>
{
    private readonly ILocationReportRepository _locationReportRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetLocationReportsQueryHandler> _logger;

    public GetLocationReportsQueryHandler(
        ILocationReportRepository locationReportRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        ILogger<GetLocationReportsQueryHandler> logger)
    {
        _locationReportRepository = locationReportRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<LocationReportDto>>> Handle(GetLocationReportsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Get location reports attempt. Status: {Status}, ReportType: {ReportType}, Page: {PageNumber}, PageSize: {PageSize}",
            request.Status ?? "all",
            request.ReportType ?? "all",
            request.PageNumber,
            request.PageSize);

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Get location reports failed: Invalid or missing user ID");
            return Result<PaginatedList<LocationReportDto>>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var query = _locationReportRepository.GetLocationReportsQuery(request.Status, request.ReportType);

        var paginatedReports = await query.ToPaginatedListAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var reportDtos = _mapper.Map<List<LocationReportDto>>(paginatedReports.Items);

        var result = new PaginatedList<LocationReportDto>(
            reportDtos,
            paginatedReports.TotalCount,
            paginatedReports.PageNumber,
            paginatedReports.PageSize);

        _logger.LogInformation(
            "Location reports retrieved successfully. TotalCount: {TotalCount}, PageNumber: {PageNumber}, PageSize: {PageSize}",
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Result<PaginatedList<LocationReportDto>>.Success(result);
    }
}
