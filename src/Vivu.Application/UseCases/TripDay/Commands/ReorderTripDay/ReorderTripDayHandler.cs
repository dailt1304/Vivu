using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay
{
    public class ReorderTripDayHandler : IRequestHandler<ReorderTripDayCommand, Result<List<TripDayResponse>>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReorderTripDayHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public ReorderTripDayHandler(
            ITripDayRepository tripDayRepository,
            ITripMemberRepository tripMemberRepository,
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReorderTripDayHandler> logger,
            ICurrentUser currentUser)
        {
            _tripDayRepository = tripDayRepository;
            _tripMemberRepository = tripMemberRepository;
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<List<TripDayResponse>>> Handle(ReorderTripDayCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Reorder TripDay failed: User not authenticated");
                return Result<List<TripDayResponse>>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Reordering trip days. UserId: {UserId}, TripId: {TripId}, TripDayCount: {TripDayCount}",
                currentUserId,
                request.TripId,
                request.OrderedTripDayIds.Count);

            // Trip exists
            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Reorder TripDay failed: Trip not found. TripId: {TripId}", request.TripId);
                return Result<List<TripDayResponse>>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Owner check
            var ownerId = await _tripMemberRepository.GetOwnerIdByTripId(request.TripId);
            if (ownerId == null)
            {
                _logger.LogWarning("Reorder TripDay failed: Trip owner not found. TripId: {TripId}", request.TripId);
                return Result<List<TripDayResponse>>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (ownerId.Value != currentUserId)
            {
                _logger.LogWarning("Reorder TripDay denied: AccessDenied. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    request.TripId, currentUserId, ownerId.Value);

                return Result<List<TripDayResponse>>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Get all trip days for this trip
            var tripDays = await _tripDayRepository.GetByTripIdAsync(request.TripId);
            if (tripDays == null || !tripDays.Any())
            {
                _logger.LogWarning(
                    "Reorder TripDay failed: No trip days found for Trip. TripId: {TripId}",
                    request.TripId);
                return Result<List<TripDayResponse>>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Validate that all provided IDs exist in the trip
            var existingIds = tripDays.Select(td => td.Id).ToHashSet();
            var invalidIds = request.OrderedTripDayIds.Where(id => !existingIds.Contains(id)).ToList();

            if (invalidIds.Any())
            {
                _logger.LogWarning(
                    "Reorder TripDay failed: Invalid trip day IDs provided. InvalidIds: {InvalidIds}",
                    string.Join(", ", invalidIds));
                return Result<List<TripDayResponse>>.Failure(new Error(
                    "TripDay.InvalidIds",
                    "One or more trip day IDs are invalid or don't belong to this trip."));
            }

            // Validate that all trip days are included in the reorder request
            if (request.OrderedTripDayIds.Count != tripDays.Count)
            {
                _logger.LogWarning(
                    "Reorder TripDay failed: Mismatch in trip day count. Expected: {Expected}, Provided: {Provided}",
                    tripDays.Count,
                    request.OrderedTripDayIds.Count);
                return Result<List<TripDayResponse>>.Failure(new Error(
                    "TripDay.InvalidCount",
                    $"Expected {tripDays.Count} trip days, but received {request.OrderedTripDayIds.Count}."));
            }

            // Update DayIndex based on the new order
            _logger.LogDebug("Updating DayIndex for {Count} trip days", request.OrderedTripDayIds.Count);
            for (int i = 0; i < request.OrderedTripDayIds.Count; i++)
            {
                var tripDayToUpdate = tripDays.First(td => td.Id == request.OrderedTripDayIds[i]);
                var oldDayIndex = tripDayToUpdate.DayIndex;
                tripDayToUpdate.DayIndex = i + 1;

                _logger.LogDebug(
                    "Updated TripDay. Id: {Id}, OldDayIndex: {OldDayIndex}, NewDayIndex: {NewDayIndex}",
                    tripDayToUpdate.Id,
                    oldDayIndex,
                    i);

                _tripDayRepository.Update(tripDayToUpdate);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Successfully reordered trip days. TripId: {TripId}, Count: {Count}",
                request.TripId,
                tripDays.Count);

            // Return the reordered list of trip days
            var orderedTripDays = request.OrderedTripDayIds
                .Select(id => tripDays.First(td => td.Id == id))
                .ToList();

            // Map to response using AutoMapper
            var response = _mapper.Map<List<TripDayResponse>>(orderedTripDays);

            _logger.LogDebug("Mapped {Count} trip days to response", response.Count);
            return Result<List<TripDayResponse>>.Success(response);
        }
    }
}