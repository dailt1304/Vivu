using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripLocation.Commands.RemoveAlternative
{
    public class RemoveAlternativeHandler : IRequestHandler<RemoveAlternativeCommand, Result>
    {
        private readonly ITripLocationAlternativeRepository _alternativeRepository;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemoveAlternativeHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public RemoveAlternativeHandler(
            ITripLocationAlternativeRepository alternativeRepository,
            ITripLocationRepository tripLocationRepository,
            ITripMemberRepository tripMemberRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveAlternativeHandler> logger,
            ICurrentUser currentUser)
        {
            _alternativeRepository = alternativeRepository;
            _tripLocationRepository = tripLocationRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(RemoveAlternativeCommand request, CancellationToken cancellationToken)
        {
            // Auth check
            if (string.IsNullOrEmpty(_currentUser.Id))
                return Result.Failure(DomainErrors.Auth.InvalidToken);

            var currentUserId = Guid.Parse(_currentUser.Id);

            // Get alternative
            var alternative = await _alternativeRepository.GetByIdAsync(request.AlternativeId);
            if (alternative == null)
            {
                _logger.LogWarning("RemoveAlternative failed: Alternative {Id} not found", request.AlternativeId);
                return Result.Failure(DomainErrors.TripLocation.AlternativeNotFound);
            }

            // Access check via parent TripLocation
            var tripLocation = await _tripLocationRepository.GetByIdAsync(alternative.TripLocationId);
            if (tripLocation == null)
                return Result.Failure(DomainErrors.TripLocation.NotFound);

            var member = await _tripMemberRepository.GetByTripAndUserAsync(
                tripLocation.TripDay.TripId, currentUserId, cancellationToken);
            if (member == null || (member.Role != "owner" && member.Role != "editor"))
                return Result.Failure(DomainErrors.Trip.AccessDenied);

            // Remove
            _alternativeRepository.Remove(alternative);

            // Reprioritize remaining alternatives
            var remaining = await _alternativeRepository.GetByTripLocationIdAsync(
                alternative.TripLocationId, cancellationToken);
            var filtered = remaining.Where(a => a.Id != request.AlternativeId).OrderBy(a => a.Priority).ToList();
            for (int i = 0; i < filtered.Count; i++)
            {
                filtered[i].Priority = i + 1;
                _alternativeRepository.Update(filtered[i]);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alternative {Id} removed from TripLocation {TripLocationId}",
                request.AlternativeId, alternative.TripLocationId);

            return Result.Success();
        }
    }
}
