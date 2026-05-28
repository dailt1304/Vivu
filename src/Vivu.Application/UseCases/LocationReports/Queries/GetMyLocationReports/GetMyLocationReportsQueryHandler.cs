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

namespace Vivu.Application.UseCases.LocationReports.Queries.GetMyLocationReports;

public class GetMyLocationReportsQueryHandler : IRequestHandler<GetMyLocationReportsQuery, Result<PaginatedList<LocationReportDto>>>
{
    private readonly ILocationReportRepository _locationReportRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetMyLocationReportsQueryHandler> _logger;

    public GetMyLocationReportsQueryHandler(
        ILocationReportRepository locationReportRepository,
        IMapper mapper,
        ICurrentUser currentUser,
        ILogger<GetMyLocationReportsQueryHandler> logger)
    {
        _locationReportRepository = locationReportRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<LocationReportDto>>> Handle(
        GetMyLocationReportsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Get my location reports attempt. Status: {Status}, ReportType: {ReportType}, Page: {PageNumber}, PageSize: {PageSize}",
            request.Status ?? "all",
            request.ReportType ?? "all",
            request.PageNumber,
            request.PageSize);

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Get my location reports failed: Invalid or missing user ID");
            return Result<PaginatedList<LocationReportDto>>.Failure(DomainErrors.Auth.InvalidToken);
        }

        _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

        var query = _locationReportRepository
            .GetLocationReportsQuery(request.Status, request.ReportType)
            .Where(r => r.UserId == userId && r.ReportType != "NEW_LOCATION");

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
            "My location reports retrieved successfully. UserId: {UserId}, TotalCount: {TotalCount}, PageNumber: {PageNumber}, PageSize: {PageSize}",
            userId,
            result.TotalCount,
            result.PageNumber,
            result.PageSize);

        return Result<PaginatedList<LocationReportDto>>.Success(result);
    }
}
