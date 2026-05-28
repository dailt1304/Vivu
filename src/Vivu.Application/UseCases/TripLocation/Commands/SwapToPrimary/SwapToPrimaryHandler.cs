using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.SwapToPrimary
{
    public class SwapToPrimaryHandler : IRequestHandler<SwapToPrimaryCommand, Result<TripLocationResponse>>
    {
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripLocationAlternativeRepository _alternativeRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SwapToPrimaryHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public SwapToPrimaryHandler(
            ITripLocationRepository tripLocationRepository,
            ITripLocationAlternativeRepository alternativeRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<SwapToPrimaryHandler> logger,
            ICurrentUser currentUser)
        {
            _tripLocationRepository = tripLocationRepository;
            _alternativeRepository = alternativeRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<TripLocationResponse>> Handle(
            SwapToPrimaryCommand request, CancellationToken cancellationToken)
        {
            // Auth check
            if (string.IsNullOrEmpty(_currentUser.Id))
                return Result<TripLocationResponse>.Failure(DomainErrors.Auth.InvalidToken);

            var currentUserId = Guid.Parse(_currentUser.Id);

            // Get TripLocation (tracked, not AsNoTracking)
            var tripLocation = await _tripLocationRepository.GetByIdAsync(request.TripLocationId);
            if (tripLocation == null)
            {
                _logger.LogWarning("SwapToPrimary failed: TripLocation {Id} not found", request.TripLocationId);
                return Result<TripLocationResponse>.Failure(
                    DomainErrors.TripLocation.NotFoundById(request.TripLocationId));
            }

            // Access check
            var member = await _tripMemberRepository.GetByTripAndUserAsync(
                tripLocation.TripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
                return Result<TripLocationResponse>.Failure(DomainErrors.Trip.AccessDenied);

            // Get alternative
            var alternative = await _alternativeRepository.GetByIdAsync(request.AlternativeId);
            if (alternative == null || alternative.TripLocationId != request.TripLocationId)
            {
                _logger.LogWarning("SwapToPrimary failed: Alternative {Id} not found or mismatch",
                    request.AlternativeId);
                return Result<TripLocationResponse>.Failure(DomainErrors.TripLocation.AlternativeNotFound);
            }

            // Swap: old primary → alternative, alternative → primary
            var oldPrimaryLocationId = tripLocation.LocationId;
            var newPrimaryLocationId = alternative.LocationId;

            // Check if oldPrimaryLocationId already exists as another alternative
            // (this can happen after a previous swap operation)
            var allAlternatives = await _alternativeRepository
                .GetByTripLocationIdAsync(request.TripLocationId, cancellationToken);
            var existingConflict = allAlternatives
                .FirstOrDefault(a => a.LocationId == oldPrimaryLocationId && a.Id != alternative.Id);
            if (existingConflict != null)
            {
                _logger.LogInformation(
                    "Removing conflicting alternative {AltId} (LocationId={LocId}) before swap",
                    existingConflict.Id, existingConflict.LocationId);
                _alternativeRepository.Remove(existingConflict);
            }

            // Clear navigation properties to prevent EF Core from overwriting FK with loaded instance
            tripLocation.Location = null!;
            tripLocation.TripDay = null!;
            alternative.Location = null!;

            // Update TripLocation's primary LocationId
            tripLocation.LocationId = newPrimaryLocationId;
            tripLocation.ModifiedDate = DateTime.UtcNow;
            _tripLocationRepository.Update(tripLocation);

            // Update alternative's LocationId to old primary
            alternative.LocationId = oldPrimaryLocationId;
            alternative.ModifiedDate = DateTime.UtcNow;
            _alternativeRepository.Update(alternative);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Swapped primary. TripLocation {TripLocationId}: {OldLocationId} → {NewLocationId}",
                request.TripLocationId, oldPrimaryLocationId, newPrimaryLocationId);

            // Reload with details for response
            var updated = await _tripLocationRepository.GetByIdWithDetailsAsync(request.TripLocationId);
            var response = _mapper.Map<TripLocationResponse>(updated);

            return Result<TripLocationResponse>.Success(response);
        }
    }
}
