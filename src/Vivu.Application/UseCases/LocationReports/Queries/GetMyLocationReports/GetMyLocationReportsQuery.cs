using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Queries.GetMyLocationReports;

public class GetMyLocationReportsQuery : PaginationRequest, IRequest<Result<PaginatedList<LocationReportDto>>>
{
    public string? Status { get; set; }
    public string? ReportType { get; set; }
}
