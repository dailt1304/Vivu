using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using TripDayEntity = Vivu.Domain.Entities.TripDay;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;
using TripMemberEntity = Vivu.Domain.Entities.TripMember;

namespace Vivu.Application.UseCases.Trips.Commands.CopyTrip
{
    public class CopyTripCommandHandler : IRequestHandler<CopyTripCommand, Result<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CopyTripCommandHandler> _logger;

        public CopyTripCommandHandler(
            ITripRepository tripRepository,
            ITripDayRepository tripDayRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IInviteCodeGenerator inviteCodeGenerator,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CopyTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripDayRepository = tripDayRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripMemberRepository = tripMemberRepository;
            _inviteCodeGenerator = inviteCodeGenerator;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<TripDto>> Handle(CopyTripCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Copy trip attempt. TripId: {TripId}",
                request.TripId);

            // Check current user
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Copy trip failed: Invalid or missing user ID");
                return Result<TripDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            // Get the original trip with all details (including trip days and trip locations)
            var originalTrip = await _tripRepository.GetTripByIdWithDetailsAsync(request.TripId, cancellationToken);

            if (originalTrip == null || originalTrip.IsDeleted)
            {
                _logger.LogWarning("Trip not found or deleted. TripId: {TripId}", request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.NotFound);
            }

            // Verify the trip is public
            if (!originalTrip.IsPublic)
            {
                _logger.LogWarning("Trip is not public. TripId: {TripId}", request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.TripIsPrivate);
            }

            _logger.LogDebug("Original trip found. Title: {Title}, Days: {DayCount}",
                originalTrip.Title, originalTrip.TripDays.Count);

            try
            {
                // Generate new invite code
                var inviteCode = await _inviteCodeGenerator.Generate(cancellationToken);
                _logger.LogDebug("Generated new invite code: {InviteCode}", inviteCode);

                // Create new trip with customizations or original values
                var newTrip = Trip.Create(
                    userId: userId,
                    title: request.Title ?? $"{originalTrip.Title} (Copy)",
                    description: request.Description ?? originalTrip.Description,
                    coverUrl: request.CoverUrl ?? originalTrip.CoverUrl,
                    startDate: request.StartDate ?? originalTrip.StartDate,
                    endDate: request.EndDate ?? originalTrip.EndDate,
                    tripSize: request.TripSize ?? originalTrip.TripSize,
                    isPublic: false, 
                    inviteCode: inviteCode,
                    cityId: originalTrip.CityId
                );

                await _tripRepository.AddAsync(newTrip);
                _logger.LogDebug("New trip created. TripId: {TripId}", newTrip.Id);

                // Create trip member for the owner
                var tripMember = new TripMemberEntity
                {
                    TripId = newTrip.Id,
                    UserId = userId,
                    OwnerId = userId,
                    Role = "owner",
                    JoinedAt = DateTime.UtcNow
                };

                await _tripMemberRepository.AddAsync(tripMember);
                _logger.LogDebug("Trip owner member added. UserId: {UserId}", userId);

                // Deep copy trip days and locations
                var dayMapping = new Dictionary<Guid, Guid>(); // Map old day IDs to new day IDs

                foreach (var originalDay in originalTrip.TripDays.OrderBy(d => d.DayIndex))
                {
                    var newDay = TripDayEntity.Create(
                        TripId: newTrip.Id,
                        Tittle: originalDay.Title,
                        DayDate: originalDay.DayDate,
                        DayIndex: originalDay.DayIndex
                    );

                    await _tripDayRepository.AddAsync(newDay);
                    dayMapping[originalDay.Id] = newDay.Id;

                    _logger.LogDebug("Trip day copied. OriginalDayId: {OriginalDayId}, NewDayId: {NewDayId}, DayIndex: {DayIndex}",
                        originalDay.Id, newDay.Id, originalDay.DayIndex);

                    // Deep copy trip locations for this day
                    var originalLocations = originalDay.TripLocations.OrderBy(l => l.OrderIndex).ToList();

                    foreach (var originalLocation in originalLocations)
                    {
                        var newLocation = TripLocationEntity.Create(
                            tripDayId: newDay.Id,
                            locationId: originalLocation.LocationId,
                            orderIndex: originalLocation.OrderIndex,
                            startTime: originalLocation.StartTime,
                            endTime: originalLocation.EndTime,
                            note: originalLocation.Note,
                            transportMode: originalLocation.TransportMode
                        );

                        await _tripLocationRepository.AddAsync(newLocation);
                    }

                    _logger.LogDebug("Copied {LocationCount} locations for day {DayIndex}",
                        originalLocations.Count, originalDay.DayIndex);
                }

                // Save all changes
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Trip copied successfully. OriginalTripId: {OriginalTripId}, NewTripId: {NewTripId}, UserId: {UserId}, Days: {DayCount}, TotalLocations: {LocationCount}",
                    request.TripId, newTrip.Id, userId,
                    originalTrip.TripDays.Count,
                    originalTrip.TripDays.Sum(d => d.TripLocations.Count));

                // Map to DTO with current user context
                var tripDto = _mapper.Map<TripDto>(newTrip, opts => opts.Items["CurrentUserId"] = userId);

                return Result<TripDto>.Success(tripDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error copying trip. TripId: {TripId}, UserId: {UserId}",
                    request.TripId, userId);

                return Result<TripDto>.Failure(DomainErrors.Trip.CreationFailed);
            }
        }
    }
}