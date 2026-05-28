using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetPublicTripDetail
{
    public class GetPublicTripDetailQueryHandler : IRequestHandler<GetPublicTripDetailQuery, Result<DetailedTripDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPublicTripDetailQueryHandler> _logger;

        public GetPublicTripDetailQueryHandler(
            ITripRepository tripRepository,
            IMapper mapper,
            ILogger<GetPublicTripDetailQueryHandler> logger)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<DetailedTripDto>> Handle(
            GetPublicTripDetailQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching public trip detail for TripId: {TripId}",
                request.TripId);

            var trip = await _tripRepository.GetTripByIdWithDetailsAsync(request.TripId, cancellationToken);

            if (trip == null)
            {
                _logger.LogWarning("Trip with ID {TripId} not found", request.TripId);
                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.NotFoundById(request.TripId));
            }

            if (!trip.IsPublic)
            {
                _logger.LogWarning("Trip {TripId} is not public", request.TripId);
                return Result<DetailedTripDto>.Failure(DomainErrors.Trip.NotPublic);
            }

            var detailedTripDto = _mapper.Map<DetailedTripDto>(trip);

            _logger.LogInformation(
                "Successfully fetched public trip {TripId} with {MemberCount} members, {DayCount} days, {LocationCount} locations",
                trip.Id,
                detailedTripDto.MemberCount,
                detailedTripDto.DayCount,
                detailedTripDto.LocationCount);

            return Result<DetailedTripDto>.Success(detailedTripDto);
        }
    }
}
