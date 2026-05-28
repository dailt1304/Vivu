using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.AddAlternative
{
    public class AddAlternativeHandler : IRequestHandler<AddAlternativeCommand, Result<TripLocationAlternativeResponse>>
    {
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripLocationAlternativeRepository _alternativeRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddAlternativeHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public AddAlternativeHandler(
            ITripLocationRepository tripLocationRepository,
            ITripLocationAlternativeRepository alternativeRepository,
            ILocationRepository locationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddAlternativeHandler> logger,
            ICurrentUser currentUser)
        {
            _tripLocationRepository = tripLocationRepository;
            _alternativeRepository = alternativeRepository;
            _locationRepository = locationRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<TripLocationAlternativeResponse>> Handle(
            AddAlternativeCommand request, CancellationToken cancellationToken)
        {
            // Auth check
            if (string.IsNullOrEmpty(_currentUser.Id))
            {
                _logger.LogWarning("AddAlternative failed: User not authenticated");
                return Result<TripLocationAlternativeResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var currentUserId = Guid.Parse(_currentUser.Id);

            // Verify TripLocation exists
            var tripLocation = await _tripLocationRepository.GetByIdAsync(request.TripLocationId);
            if (tripLocation == null)
            {
                _logger.LogWarning("AddAlternative failed: TripLocation {Id} not found", request.TripLocationId);
                return Result<TripLocationAlternativeResponse>.Failure(
                    DomainErrors.TripLocation.NotFoundById(request.TripLocationId));
            }

            // Access check (owner or editor)
            var member = await _tripMemberRepository.GetByTripAndUserAsync(
                tripLocation.TripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
            {
                _logger.LogWarning("AddAlternative failed: Access denied for user {UserId}", currentUserId);
                return Result<TripLocationAlternativeResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Check max alternatives limit
            var currentCount = await _alternativeRepository.CountByTripLocationIdAsync(
                request.TripLocationId, cancellationToken);
            if (currentCount >= Domain.Entities.TripLocation.MaxAlternatives)
            {
                _logger.LogWarning("AddAlternative failed: Max alternatives reached for TripLocation {Id}",
                    request.TripLocationId);
                return Result<TripLocationAlternativeResponse>.Failure(
                    DomainErrors.TripLocation.MaxAlternativesReached);
            }

            // Check duplicate
            var exists = await _alternativeRepository.ExistsAsync(
                request.TripLocationId, request.LocationId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("AddAlternative failed: Location {LocationId} already exists as alternative",
                    request.LocationId);
                return Result<TripLocationAlternativeResponse>.Failure(
                    DomainErrors.TripLocation.AlternativeAlreadyExists);
            }

            // Verify Location exists
            var location = await _locationRepository.GetByIdAsync(request.LocationId);
            if (location == null)
            {
                _logger.LogWarning("AddAlternative failed: Location {Id} not found", request.LocationId);
                return Result<TripLocationAlternativeResponse>.Failure(
                    DomainErrors.Location.NotFoundById(request.LocationId));
            }

            // Create alternative
            var priority = currentCount + 1;
            var alternative = TripLocationAlternative.Create(
                request.TripLocationId, request.LocationId, priority, request.Reason);

            await _alternativeRepository.AddAsync(alternative);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Alternative added. TripLocationId: {TripLocationId}, LocationId: {LocationId}, Priority: {Priority}",
                request.TripLocationId, request.LocationId, priority);

            // Load navigation for mapping
            alternative.Location = location;

            var response = _mapper.Map<TripLocationAlternativeResponse>(alternative);
            return Result<TripLocationAlternativeResponse>.Success(response);
        }
    }
}
