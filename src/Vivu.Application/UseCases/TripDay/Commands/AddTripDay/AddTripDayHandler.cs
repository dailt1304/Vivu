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

namespace Vivu.Application.UseCases.TripDay.Commands.AddTripDay
{
    public class AddTripDayHandler : IRequestHandler<AddTripDayCommand, Result<TripDayResponse>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddTripDayHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public AddTripDayHandler(
            ITripRepository tripRepository,
            ITripDayRepository tripDayRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddTripDayHandler> logger,
            ICurrentUser currentUser)
        {
            _tripRepository = tripRepository;
            _tripDayRepository = tripDayRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result<TripDayResponse>> Handle(AddTripDayCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Add TripDay failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<TripDayResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Adding TripDay. UserId: {UserId}, TripId: {TripId}", currentUserId, request.TripId);

            // Trip exists
            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Add TripDay failed: Trip not found. TripId: {TripId}", request.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            // Owner check
            var ownerId = await _tripMemberRepository.GetOwnerIdByTripId(request.TripId);
            if (ownerId == null)
            {
                _logger.LogWarning("Add TripDay failed: Trip owner not found. TripId: {TripId}", request.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (ownerId.Value != currentUserId)
            {
                _logger.LogWarning("Add TripDay denied: AccessDenied. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    request.TripId, currentUserId, ownerId.Value);

                return Result<TripDayResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }

            int newDayIndex;
            if (request.DayIndex.HasValue)
            {
                newDayIndex = request.DayIndex.Value;
            }
            else
            {
                var maxDayIndex = await _tripDayRepository.GetMaxIndexByTripIdAsync(request.TripId);
                newDayIndex = maxDayIndex + 1;
            }


            // Determine DateTime
            DateTime? dayDate = null;
            if (newDayIndex == 0)
            {
                dayDate = null;
            } else
            {
                if (trip.StartDate.HasValue)
                {
                    dayDate = trip.StartDate.Value.Date.AddDays(newDayIndex - 1);
                }
                else
                {
                    dayDate = request.DayDate?.Date;
                }
            }

            // Validate dayDate in range from StartDate to EndDate
            if (dayDate.HasValue)
            {
                if (trip.StartDate.HasValue && dayDate.Value < trip.StartDate.Value.Date)
                {
                    _logger.LogWarning("Add TripDay failed: DayDate {DayDate} is before Trip StartDate {StartDate}. TripId: {TripId}",
                        dayDate.Value, trip.StartDate.Value.Date, request.TripId);
                    return Result<TripDayResponse>.Failure(DomainErrors.Trip.DateInvalid);
                }
                if (trip.EndDate.HasValue && dayDate.Value > trip.EndDate.Value.Date)
                {
                    _logger.LogWarning("Add TripDay failed: DayDate {DayDate} is after Trip EndDate {EndDate}. TripId: {TripId}",
                        dayDate.Value, trip.EndDate.Value.Date, request.TripId);
                    return Result<TripDayResponse>.Failure(DomainErrors.Trip.DateInvalid);
                }
            }

            // Create TripDay
            var tripDay = Domain.Entities.TripDay.Create(
                TripId: request.TripId,
                Tittle: request.Title,
                DayIndex: newDayIndex,
                DayDate: dayDate
            );
            var createdTripDay = await _tripDayRepository.AddAsync(tripDay);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to Response
            var tripDayResponse = _mapper.Map<TripDayResponse>(createdTripDay);
            _logger.LogInformation("TripDay added successfully. TripDayId: {TripDayId}, TripId: {TripId}, UserId: {UserId}",
                tripDayResponse.Id, request.TripId, currentUserId);

            return Result<TripDayResponse>.Success(tripDayResponse);
        }
    }
}
