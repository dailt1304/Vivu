using MediatR;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.ReportLocation;

public class ReportLocationCommand : IRequest<Result<ReportLocationResponse>>
{
    public Guid LocationId { get; set; }
    public ReportType ReportType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SuggestedName { get; set; }
    public string? SuggestedAddress { get; set; }
    public List<Microsoft.AspNetCore.Http.IFormFile>? EvidenceImages { get; set; }
}
