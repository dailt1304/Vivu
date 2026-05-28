using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationReports.Commands.UpdateLocationReport
{
    public class UpdateLocationReportCommand : IRequest<Result<LocationReportDto>>
    {
        public Guid ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ReportReason { get; set; } = string.Empty;
        public string? ReportDescription { get; set; }
        public string? SuggestedName { get; set; }
        public string? SuggestedAddress { get; set; }
        public IFormFileCollection? EvidenceImages { get; set; }
    }
}
