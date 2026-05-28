using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Statictis.Queries.GetLocationStats
{
    public class GetLocationStatsQueryHandler : IRequestHandler<GetLocationStatsQuery, Result<LocationStatsDto>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly ILocationReportRepository _locationReportRepository;
        private readonly ILogger<GetLocationStatsQueryHandler> _logger;

        public GetLocationStatsQueryHandler(
            ILocationRepository locationRepository,
            ILocationReportRepository locationReportRepository,
            ILogger<GetLocationStatsQueryHandler> logger)
        {
            _locationRepository = locationRepository;
            _locationReportRepository = locationReportRepository;
            _logger = logger;
        }

        public async Task<Result<LocationStatsDto>> Handle(
            GetLocationStatsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching location statistics for admin dashboard");

            var statsDto = new LocationStatsDto
            {
                LocationsByCategory = await _locationRepository.GetLocationsByCategoryAsync(cancellationToken),
                PendingSubmissionsCount = await _locationRepository.GetPendingSubmissionsCountAsync(cancellationToken),
                ReportsByType = await _locationReportRepository.GetReportsByTypeAsync(cancellationToken),
                VerificationRate = await _locationRepository.GetVerificationRateAsync(cancellationToken),
                TotalVerifiedLocations = await _locationRepository.GetTotalVerifiedLocationsAsync(cancellationToken)
            };

            _logger.LogInformation(
                "Successfully fetched location statistics: {CategoryCount} categories, {PendingCount} pending, {ReportTypeCount} report types, {VerificationRate}% verification rate, {TotalVerified} verified locations",
                statsDto.LocationsByCategory.Count,
                statsDto.PendingSubmissionsCount,
                statsDto.ReportsByType.Count,
                statsDto.VerificationRate,
                statsDto.TotalVerifiedLocations);

            return Result<LocationStatsDto>.Success(statsDto);
        }
    }
}
