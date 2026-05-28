using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility
{
    public class UpdateTripVisibilityCommandHandler : IRequestHandler<UpdateTripVisibilityCommand, Result<TripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<UpdateTripVisibilityCommandHandler> _logger;

        public UpdateTripVisibilityCommandHandler(
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<UpdateTripVisibilityCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<TripDto>> Handle(
            UpdateTripVisibilityCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Update trip visibility failed: Invalid or missing user ID");
                return Result<TripDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            _logger.LogInformation(
                "Updating trip visibility {TripId} to {IsPublic} by user {UserId}",
                request.TripId,
                request.IsPublic,
                userId);

            var trip = await _tripRepository.GetByIdAsync(request.TripId);

            if (trip == null)
            {
                _logger.LogWarning("Trip with ID {TripId} not found", request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (trip.IsDeleted)
            {
                _logger.LogWarning("Trip with ID {TripId} has been deleted", request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.AlreadyDeleted(request.TripId));
            }

            // Only owner can change visibility
            if (!trip.CanBeDeletedBy(userId))
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update visibility of trip {TripId} without ownership",
                    userId,
                    request.TripId);
                return Result<TripDto>.Failure(DomainErrors.Trip.AccessDenied);
            }

            // If making public, validate trip is complete enough to share
            if (request.IsPublic && !trip.IsCompleteForSharing())
            {
                _logger.LogWarning(
                    "Trip {TripId} is incomplete for sharing",
                    request.TripId);
                return Result<TripDto>.Failure(
                    DomainErrors.Trip.TripIncomplete("Trip must have title, description, start date, and end date to be made public."));
            }

            if (request.IsPublic)
            {
                trip.MakePublic();
            }
            else
            {
                trip.MakePrivate();
            }

            _tripRepository.Update(trip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated trip {TripId} visibility to {IsPublic}",
                request.TripId,
                request.IsPublic);

            var tripDto = _mapper.Map<TripDto>(trip, opts => opts.Items["CurrentUserId"] = userId);
            return Result<TripDto>.Success(tripDto);
        }
    }
}
