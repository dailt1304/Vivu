using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTrip
{
    public class UpdateTripCommandHandler : IRequestHandler<UpdateTripCommand, Result<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<UpdateTripCommandHandler> _logger;

        public UpdateTripCommandHandler(
            ITripRepository tripRepository,
            ITripDayRepository tripDayRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<UpdateTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _tripDayRepository = tripDayRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<TripDto>> Handle(
            UpdateTripCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Update trip failed: Invalid or missing user ID");
                return Result<TripDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Updating trip {TripId} by user {UserId}",
                request.TripId,
                userId);

            var trip = await _tripRepository.GetTripWithMembersAsync(request.TripId, cancellationToken);

            if (trip == null)
            {
                _logger.LogWarning("Trip with ID {TripId} not found", request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (!trip.CanBeModifiedBy(userId, trip.TripMembers))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update trip {TripId} without permission",
                    userId,
                    request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.AccessDenied);
            }

            trip.Update(
                title: request.Title,
                description: request.Description,
                startDate: request.StartDate,
                endDate: request.EndDate,
                coverUrl: request.CoverUrl,
                tripSize: request.TripSize,
                status: request.Status);

            if (!trip.IsValidDateRange())
            {
                _logger.LogWarning(
                    "Invalid date range for trip {TripId}: StartDate={StartDate}, EndDate={EndDate}",
                    request.TripId,
                    request.StartDate,
                    request.EndDate);
                return Result<TripDto>.Failure(DomainErrors.Trip.DateInvalid);
            }

            var newStartDate = request.StartDate ?? trip.StartDate;
            var newEndDate = request.EndDate ?? trip.EndDate;

            if (newStartDate.HasValue && newEndDate.HasValue)
            {
                var existingTripDays = await _tripDayRepository.GetByTripIdAsync(request.TripId);
                
                if (existingTripDays == null)
                {
                    _logger.LogWarning("Failed to retrieve TripDays for trip {TripId}", request.TripId);
                    return Result<TripDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
                }
                
                var expectedTotalDays = (newEndDate.Value.Date - newStartDate.Value.Date).Days + 1;
                var currentTotalDays = existingTripDays.Count;

                _logger.LogDebug(
                    "Syncing TripDays for trip {TripId}. Expected: {ExpectedDays}, Current: {CurrentDays}",
                    request.TripId, expectedTotalDays, currentTotalDays);

                foreach (var tripDay in existingTripDays.OrderBy(td => td.DayIndex))
                {
                    if (tripDay.DayIndex <= expectedTotalDays)
                    {
                        var newDayDate = newStartDate.Value.Date.AddDays(tripDay.DayIndex - 1);
                        tripDay.Update(dayDate: DateTime.SpecifyKind(newDayDate, DateTimeKind.Utc));
                    }
                }

                if (expectedTotalDays > currentTotalDays)
                {
                    for (int i = currentTotalDays + 1; i <= expectedTotalDays; i++)
                    {
                        var dayDate = newStartDate.Value.Date.AddDays(i - 1);
                        var tripDay = Domain.Entities.TripDay.Create(
                            TripId: trip.Id,
                            Tittle: $"Day {i}",
                            DayDate: DateTime.SpecifyKind(dayDate, DateTimeKind.Utc),
                            DayIndex: i
                        );
                        await _tripDayRepository.AddAsync(tripDay);
                        _logger.LogDebug("Created new TripDay {TripDayId} for day {DayIndex}", tripDay.Id, i);
                    }
                }
                else if (expectedTotalDays < currentTotalDays)
                {
                    var tripDaysToRemove = existingTripDays
                        .Where(td => td.DayIndex > expectedTotalDays)
                        .ToList();

                    foreach (var tripDay in tripDaysToRemove)
                    {
                        _tripDayRepository.Remove(tripDay);
                        _logger.LogDebug("Removed TripDay {TripDayId} for day {DayIndex}", tripDay.Id, tripDay.DayIndex);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated trip {TripId} by user {UserId}",
                request.TripId,
                userId);

            var tripDto = _mapper.Map<TripDto>(trip, opts => 
                opts.Items["CurrentUserId"] = userId);
            return Result<TripDto>.Success(tripDto);
        }
    }
}
