using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations
{
    public class ReorderTripLocationHandler : IRequestHandler<ReorderTripLocationsCommand, Result<List<TripLocationResponse>>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReorderTripLocationHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public ReorderTripLocationHandler(
            ITripDayRepository tripDayRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReorderTripLocationHandler> logger,
            ICurrentUser currentUser)
        {
            _tripDayRepository = tripDayRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }
        public async Task<Result<List<TripLocationResponse>>> Handle(ReorderTripLocationsCommand request, CancellationToken cancellationToken)
        {
            // Check user authentication
            if (string.IsNullOrEmpty(_currentUser.Id))
            {
                _logger.LogWarning("Reorder location failed: User not authenticated");
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            // Parse current user ID to Guid
            var currentUserId = Guid.Parse(_currentUser.Id);
            _logger.LogInformation(
                "Reordering trip locations. UserId: {UserId}, TripDayId: {TripDayId}, LocationCount: {LocationCount}",
                currentUserId,
                request.TripDayId,
                request.OrderedTripLocationIds.Count);

            // Verify TripDay exists
            var tripDay = await _tripDayRepository.GetByIdAsync(request.TripDayId);
            if (tripDay == null)
            {
                _logger.LogWarning(
                    "Reorder location failed: TripDay not found. TripDayId: {TripDayId}",
                    request.TripDayId);
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.TripDay.NotFoundById(request.TripDayId));
            }
            _logger.LogDebug("TripDay found. TripDayId: {TripDayId}, TripId: {TripId}", tripDay.Id, tripDay.TripId);

            // Check if user has access to this trip (owner or member)
            var member = await _tripMemberRepository.GetByTripAndUserAsync(tripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
            {
                _logger.LogWarning(
                    "Reorder location failed: Access denied. TripId: {TripId}, UserId: {UserId}, Role: {Role}",
                    tripDay.TripId,
                    currentUserId,
                    member?.Role ?? "none");
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.Trip.AccessDenied);
            }
            _logger.LogDebug("Access verified. UserId: {UserId} is {Role} of TripId: {TripId}", currentUserId, member.Role, tripDay.TripId);
            // Get all trip locations for this trip day
            var tripLocations = await _tripLocationRepository.GetTripLocationsByTripDayIdAsync(request.TripDayId);
            if (tripLocations == null || !tripLocations.Any())
            {
                _logger.LogWarning(
                    "Reorder location failed: No locations found for TripDay. TripDayId: {TripDayId}",
                    request.TripDayId);
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.TripLocation.NotFound);
            }

            // Validate that all provided IDs exist in the trip day
            var existingIds = tripLocations.Select(tl => tl.Id).ToHashSet();
            var invalidIds = request.OrderedTripLocationIds.Where(id => !existingIds.Contains(id)).ToList();
            
            if (invalidIds.Any())
            {
                _logger.LogWarning(
                    "Reorder location failed: Invalid trip location IDs provided. InvalidIds: {InvalidIds}",
                    string.Join(", ", invalidIds));
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.TripLocation.InvalidIds);
            }

            // Validate that all trip locations are included in the reorder request
            if (request.OrderedTripLocationIds.Count != tripLocations.Count)
            {
                _logger.LogWarning(
                    "Reorder location failed: Mismatch in location count. Expected: {Expected}, Provided: {Provided}",
                    tripLocations.Count,
                    request.OrderedTripLocationIds.Count);
                return Result<List<TripLocationResponse>>.Failure(DomainErrors.TripLocation.InvalidCount);
            }

            // Update order indices and swap time slots
            _logger.LogDebug("Updating order indices and swapping time slots for {Count} locations", request.OrderedTripLocationIds.Count);

            // Capture original time slots in their current order
            var originalTimeSlots = tripLocations
                .OrderBy(tl => tl.OrderIndex)
                .Select(tl => new { tl.StartTime, tl.EndTime })
                .ToList();
            for (int i = 0; i < request.OrderedTripLocationIds.Count; i++)
            {
                var tripLocation = tripLocations.First(tl => tl.Id == request.OrderedTripLocationIds[i]);
                var oldOrderIndex = tripLocation.OrderIndex;
                // Update OrderIndex (1-based)
                tripLocation.OrderIndex = i + 1;

                // Swap time slots: take the time slot that was at this position originally
                if (i < originalTimeSlots.Count)
                {
                    tripLocation.StartTime = originalTimeSlots[i].StartTime;
                    tripLocation.EndTime = originalTimeSlots[i].EndTime;
                }

                _logger.LogDebug(
                    "Updated TripLocation. Id: {Id}, OldOrderIndex: {OldOrderIndex}, NewOrderIndex: {NewOrderIndex}, Time: {StartTime}-{EndTime}",
                    tripLocation.Id,
                    oldOrderIndex,
                    tripLocation.OrderIndex,
                    tripLocation.StartTime,
                    tripLocation.EndTime);

                _tripLocationRepository.Update(tripLocation);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Successfully reordered trip locations. TripDayId: {TripDayId}, Count: {Count}",
                request.TripDayId,
                tripLocations.Count);

            // Load locations for mapping
            var orderedLocations = request.OrderedTripLocationIds
                .Select(id => tripLocations.First(tl => tl.Id == id))
                .ToList();

            // Map to response using AutoMapper
            var response = _mapper.Map<List<TripLocationResponse>>(orderedLocations);

            _logger.LogDebug("Mapped {Count} trip locations to response", response.Count);
            return Result<List<TripLocationResponse>>.Success(response);
        }
    }
}
