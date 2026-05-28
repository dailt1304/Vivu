using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay
{
    public class RemoveTripDayHandler : IRequestHandler<RemoveTripDayCommand, Result<TripDayResponse>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<RemoveTripDayHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public RemoveTripDayHandler(
            ITripDayRepository tripDayRepository,
            ITripLocationRepository tripLocationRepository,
            ITripRepository tripRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<RemoveTripDayHandler> logger,
            ICurrentUser currentUser)
        {
            _tripDayRepository = tripDayRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<TripDayResponse>> Handle(RemoveTripDayCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Remove TripDay failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<TripDayResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Removing TripDay. UserId: {UserId}, TripDayId: {TripDayId}", currentUserId, request.TripDayId);

            // TripDay exists
            var tripDay = await _tripDayRepository.GetByIdAsync(request.TripDayId);
            if (tripDay == null)
            {
                _logger.LogWarning("Remove TripDay failed: TripDay not found. TripDayId: {TripDayId}", request.TripDayId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundByTripDay(request.TripDayId));
            }

            // Trip exists - fetch separately since GetByIdAsync doesn't include navigation props
            var trip = await _tripRepository.GetByIdAsync(tripDay.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Remove TripDay failed: Trip not found. TripId: {TripId}", tripDay.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(tripDay.TripId));
            }

            // Owner check
            var ownerId = await _tripMemberRepository.GetOwnerIdByTripId(tripDay.TripId);
            if (ownerId == null)
            {
                _logger.LogWarning("Remove TripDay failed: Trip owner not found. TripId: {TripId}", tripDay.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(tripDay.TripId));
            }

            if (ownerId.Value != currentUserId)
            {
                _logger.LogWarning("Remove TripDay denied: AccessDenied. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    tripDay.TripId, currentUserId, ownerId.Value);

                return Result<TripDayResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Map to Response before deleting
            var tripDayResponse = _mapper.Map<TripDayResponse>(tripDay);

            // Delete all TripLocations related to this TripDay
            var tripLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(tripDay.Id);
            var locationCount = tripLocations.Count;
            
            if (locationCount > 0)
            {
                foreach (var location in tripLocations)
                {
                    _tripLocationRepository.Remove(location);
                }
                _logger.LogInformation("Removing {Count} TripLocations from TripDay {TripDayId}", 
                    locationCount, tripDay.Id);
            }

            // Delete TripDay
            _tripDayRepository.Remove(tripDay);
            
            // Reorder DayIndex after deletion
            await _tripDayRepository.ReorderDayIndexAfterUpdateAsync(tripDay.TripId, tripDay.DayIndex);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TripDay removed successfully with {LocationCount} locations. TripDayId: {TripDayId}, TripId: {TripId}, UserId: {UserId}",
                locationCount, request.TripDayId, tripDay.TripId, currentUserId);

            return Result<TripDayResponse>.Success(tripDayResponse);
        }
    }
}
