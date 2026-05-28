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

namespace Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay
{
    public class UpdateTripDayHandler : IRequestHandler<UpdateTripDayCommand, Result<TripDayResponse>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ITripDayRepository _tripDayRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateTripDayHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public UpdateTripDayHandler(
            ITripRepository tripRepository,
            ITripDayRepository tripDayRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateTripDayHandler> logger,
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

        public async Task<Result<TripDayResponse>> Handle(UpdateTripDayCommand request, CancellationToken cancellationToken)
        {
            // Auth
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                _logger.LogWarning("Update TripDay failed: user not authenticated/invalid id. TraceId: {TraceId}", _currentUser.TraceId);
                return Result<TripDayResponse>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation("Updating TripDay. UserId: {UserId}, TripDayId: {TripDayId}", currentUserId, request.TripDayId);

            // TripDay exists
            var tripDay = await _tripDayRepository.GetByIdAsync(request.TripDayId);
            if (tripDay == null)
            {
                _logger.LogWarning("Update TripDay failed: TripDay not found. TripDayId: {TripDayId}", request.TripDayId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundByTripDay(request.TripDayId));
            }

            // Trip exists
            var trip = await _tripRepository.GetByIdAsync(tripDay.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Update TripDay failed: Trip not found. TripId: {TripId}", tripDay.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(tripDay.TripId));
            }

            // Owner check
            var ownerId = await _tripMemberRepository.GetOwnerIdByTripId(tripDay.TripId);
            if (ownerId == null)
            {
                _logger.LogWarning("Update TripDay failed: Trip owner not found. TripId: {TripId}", tripDay.TripId);
                return Result<TripDayResponse>.Failure(DomainErrors.Trip.NotFoundById(tripDay.TripId));
            }

            if (ownerId.Value != currentUserId)
            {
                _logger.LogWarning("Update TripDay denied: AccessDenied. TripId: {TripId}, UserId: {UserId}, OwnerId: {OwnerId}",
                    tripDay.TripId, currentUserId, ownerId.Value);

                return Result<TripDayResponse>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // Validate DayDate if provided
            if (request.DayDate.HasValue)
            {
                var newDayDate = request.DayDate.Value.Date;

                // Validate dayDate in range from StartDate to EndDate
                if (trip.StartDate.HasValue && newDayDate < trip.StartDate.Value.Date)
                {
                    _logger.LogWarning("Update TripDay failed: DayDate {DayDate} is before Trip StartDate {StartDate}. TripId: {TripId}",
                        newDayDate, trip.StartDate.Value.Date, tripDay.TripId);
                    return Result<TripDayResponse>.Failure(DomainErrors.Trip.DateInvalid);
                }

                if (trip.EndDate.HasValue && newDayDate > trip.EndDate.Value.Date)
                {
                    _logger.LogWarning("Update TripDay failed: DayDate {DayDate} is after Trip EndDate {EndDate}. TripId: {TripId}",
                        newDayDate, trip.EndDate.Value.Date, tripDay.TripId);
                    return Result<TripDayResponse>.Failure(DomainErrors.Trip.DateInvalid);
                }
            }

            // Update TripDay
            tripDay.Update(title: request.Title, dayDate: request.DayDate);
            _tripDayRepository.Update(tripDay);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to Response
            var tripDayResponse = _mapper.Map<TripDayResponse>(tripDay);
            _logger.LogInformation("TripDay updated successfully. TripDayId: {TripDayId}, TripId: {TripId}, UserId: {UserId}",
                tripDay.Id, tripDay.TripId, currentUserId);

            return Result<TripDayResponse>.Success(tripDayResponse);
        }
    }
}