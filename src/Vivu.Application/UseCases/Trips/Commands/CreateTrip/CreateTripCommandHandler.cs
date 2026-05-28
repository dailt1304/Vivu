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

namespace Vivu.Application.UseCases.Trips.Commands.CreateTrip
{
    public class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Result<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly IInviteCodeGenerator _inviteCodeGenerator;
        private readonly ITripLimitChecker _tripLimitChecker;
        private readonly ILogger<CreateTripCommandHandler> _logger;

        public CreateTripCommandHandler(
            ITripRepository tripRepository,
            ITripMemberRepository tripMemberRepository,
            ITripDayRepository tripDayRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUser currentUser,
            IInviteCodeGenerator inviteCodeGenerator,
            ITripLimitChecker tripLimitChecker,
            ILogger<CreateTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _tripDayRepository = tripDayRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _inviteCodeGenerator = inviteCodeGenerator;
            _tripLimitChecker = tripLimitChecker;
            _logger = logger;
        }

        public async Task<Result<TripDto>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Create trip attempt. Title: {Title}, IsPublic: {IsPublic}, GenerateInviteCode: {GenerateInviteCode}",
                request.Title,
                request.IsPublic,
                request.GenerateInviteCode);

            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Create trip failed: Invalid or missing user ID");
                return Result<TripDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogDebug("User authenticated. UserId: {UserId}", userId);

            var canCreateTrip = await _tripLimitChecker.CanCreateTripAsync(userId, cancellationToken);
            if (!canCreateTrip)
            {
                _logger.LogWarning(
                    "Create trip failed: Trip limit reached. UserId: {UserId}",
                    userId);
                return Result<TripDto>.Failure(DomainErrors.Trip.TripLimitReached);
            }

            string? inviteCode = null;
            if (request.GenerateInviteCode)
            {
                inviteCode = await _inviteCodeGenerator.Generate(cancellationToken);
                _logger.LogDebug("Invite code generated: {InviteCode}", inviteCode);
            }

            var trip = Trip.Create(
                userId: userId,
                title: request.Title,
                description: request.Description,
                coverUrl: request.CoverUrl,
                startDate: request.StartDate,
                endDate: request.EndDate,
                tripSize: request.TripSize,
                isPublic: request.IsPublic,
                inviteCode: inviteCode,
                cityId: request.CityId);

            await _tripRepository.AddAsync(trip);
            _logger.LogDebug("Trip entity created. TripId: {TripId}", trip.Id);

            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: userId,
                ownerId: userId,
                role: "owner");
            await _tripMemberRepository.AddAsync(tripMember);
            _logger.LogDebug("TripMember created for owner. TripId: {TripId}, UserId: {UserId}", trip.Id, userId);

            // Create "Ideas" day (DayIndex 0)
            var ideasDay = Domain.Entities.TripDay.Create(
                TripId: trip.Id,
                Tittle: "Ideas",
                DayIndex: 0
            );
            await _tripDayRepository.AddAsync(ideasDay);
            _logger.LogDebug("Created 'Ideas' TripDay (DayIndex 0) for trip {TripId}", trip.Id);

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                var totalDays = (request.EndDate.Value.Date - request.StartDate.Value.Date).Days + 1;
                _logger.LogDebug("Creating {TotalDays} TripDays for trip {TripId}", totalDays, trip.Id);

                for (int i = 0; i < totalDays; i++)
                {
                    var dayDate = request.StartDate.Value.Date.AddDays(i);
                    var tripDay = Domain.Entities.TripDay.Create(
                        TripId: trip.Id,
                        Tittle: $"Day {i + 1}",
                        DayDate: DateTime.SpecifyKind(dayDate, DateTimeKind.Utc),
                        DayIndex: i + 1
                    );
                    await _tripDayRepository.AddAsync(tripDay);
                    _logger.LogDebug("Created TripDay {TripDayId} for day {DayIndex}", tripDay.Id, i + 1);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Trip saved to database. TripId: {TripId}", trip.Id);

            var tripDto = _mapper.Map<TripDto>(trip);
            _logger.LogInformation(
                "Trip created successfully. TripId: {TripId}, Title: {Title}, UserId: {UserId}, Status: {Status}",
                trip.Id,
                trip.Title,
                userId,
                trip.Status);

            return Result<TripDto>.Success(tripDto);
        }
    }
}
