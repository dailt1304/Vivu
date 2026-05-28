using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Queries.GetUserSubmittedLocations
{
    public class GetUserSubmittedLocationsQueryHandler : IRequestHandler<GetUserSubmittedLocationsQuery, Result<PaginatedList<LocationDto>>>
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<GetUserSubmittedLocationsQueryHandler> _logger;

        public GetUserSubmittedLocationsQueryHandler(
            ILocationRepository locationRepository,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<GetUserSubmittedLocationsQueryHandler> logger)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<LocationDto>>> Handle(
            GetUserSubmittedLocationsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Get user submitted locations attempt. Status: {Status}, Page: {PageNumber}, PageSize: {PageSize}",
                request.Status?.ToString() ?? "all",
                request.PageNumber,
                request.PageSize);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Get user submitted locations failed: Invalid or missing user ID");
                return Result<PaginatedList<LocationDto>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            var query = _locationRepository.GetUserReportedLocationsQuery(userId, request.Status);

            var totalBeforePagination = await query.CountAsync(cancellationToken);

            if (totalBeforePagination == 0)
            {
                _logger.LogInformation("No submitted locations found for user {UserId}", userId);
                
                var emptyResult = new PaginatedList<LocationDto>(
                    new List<LocationDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);
                
                return Result<PaginatedList<LocationDto>>.Success(emptyResult);
            }

            query = query.OrderByDescending(l => l.CreatedDate);

            var paginatedLocations = await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var locationDtos = _mapper.Map<List<LocationDto>>(paginatedLocations.Items);

            var result = new PaginatedList<LocationDto>(
                locationDtos,
                paginatedLocations.TotalCount,
                paginatedLocations.PageNumber,
                paginatedLocations.PageSize);

            _logger.LogInformation(
                "User submitted locations retrieved successfully. UserId: {UserId}, TotalCount: {TotalCount}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                userId,
                result.TotalCount,
                result.PageNumber,
                result.PageSize);

            return Result<PaginatedList<LocationDto>>.Success(result);
        }
    }
}
