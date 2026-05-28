using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Commands.UpdateTripStatus
{
    public class UpdateTripStatusCommandHandler : IRequestHandler<UpdateTripStatusCommand, Result<int>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateTripStatusCommandHandler> _logger;

        public UpdateTripStatusCommandHandler(
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateTripStatusCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<int>> Handle(
            UpdateTripStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting automatic trip status update");

            var updatedCount = 0;

            var tripsToCheck = await _tripRepository.GetTripsForStatusUpdateAsync(cancellationToken);

            _logger.LogDebug("Found {Count} trips to check for status update", tripsToCheck.Count);

            var currentUtc = DateTime.UtcNow;
            _logger.LogInformation("Current UTC time: {CurrentUtc}", currentUtc);

            foreach (var trip in tripsToCheck)
            {
                var originalStatus = trip.Status;

                _logger.LogDebug(
                    "Checking trip {TripId} '{Title}': Status={Status}, StartDate={StartDate} (Kind={StartKind}), EndDate={EndDate} (Kind={EndKind})",
                    trip.Id,
                    trip.Title,
                    trip.Status,
                    trip.StartDate,
                    trip.StartDate?.Kind,
                    trip.EndDate,
                    trip.EndDate?.Kind);

                var shouldBeOngoing = trip.ShouldBeOngoing();
                var shouldBeCompleted = trip.ShouldBeCompleted();

                _logger.LogDebug(
                    "Trip {TripId}: ShouldBeOngoing={ShouldBeOngoing}, ShouldBeCompleted={ShouldBeCompleted}",
                    trip.Id,
                    shouldBeOngoing,
                    shouldBeCompleted);

                if (shouldBeOngoing)
                {
                    trip.UpdateStatus("ongoing");
                    updatedCount++;
                    _logger.LogInformation(
                        "Trip {TripId} status updated: {OldStatus} → ongoing",
                        trip.Id,
                        originalStatus);
                }
                else if (shouldBeCompleted)
                {
                    trip.UpdateStatus("completed");
                    updatedCount++;
                    _logger.LogInformation(
                        "Trip {TripId} status updated: {OldStatus} → completed",
                        trip.Id,
                        originalStatus);
                }
            }

            if (updatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Successfully updated {Count} trip statuses",
                    updatedCount);
            }
            else
            {
                _logger.LogInformation("No trips needed status update");
            }

            return Result<int>.Success(updatedCount);
        }
    }
}
