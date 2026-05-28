using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using static Vivu.Domain.Errors.DomainErrors;

namespace Vivu.Application.UseCases.AI.ApplyTripModification
{
    public class ApplyTripModificationCommandHandler
    : IRequestHandler<ApplyTripModificationCommand, Result<DetailedTripDto>>
    {
        private readonly ITripDayRepository _tripDayRepository;
        private readonly IAIConvert _aiConvert;
        private readonly ITripLocationRepository _tripLocationRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITripRepository _tripRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ApplyTripModificationCommandHandler> _logger;
        private readonly ITripLocationAlternativeRepository _tripLocationAlternativeRepository;
        private readonly ITripItineraryOptimizer _optimizer;

        private const int MaxRetryAttempts = 3;

        public ApplyTripModificationCommandHandler(ITripDayRepository tripDayRepository, IAIConvert aiConvert, 
            ITripLocationRepository tripLocationRepository, ICurrentUser currentUser, 
            ILocationRepository locationRepository,
            IChatMessageRepository chatMessageRepository, IUnitOfWork unitOfWork, 
            ITripRepository tripRepository, IMapper mapper, ILogger<ApplyTripModificationCommandHandler> logger,
            ITripLocationAlternativeRepository tripLocationAlternativeRepository, ITripItineraryOptimizer optimizer)
        {
            _tripDayRepository = tripDayRepository;
            _aiConvert = aiConvert;
            _locationRepository = locationRepository;
            _tripLocationRepository = tripLocationRepository;
            _currentUser = currentUser;
            _chatMessageRepository = chatMessageRepository;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _tripRepository = tripRepository;
            _mapper = mapper;
            _tripLocationAlternativeRepository = tripLocationAlternativeRepository;
            _optimizer = optimizer;
        }

        public async Task<Result<DetailedTripDto>> Handle(
            ApplyTripModificationCommand request,
            CancellationToken cancellationToken)
        {
            var affectedDayIndices = new HashSet<int>();
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<DetailedTripDto>.Failure(DomainErrors.Auth.InvalidToken);

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    _unitOfWork.ClearChangeTracker();
                    var trip = await _tripRepository.GetTripByIdWithDetailsForAIModifyAsync(request.TripId, cancellationToken);
                    if (trip == null)
                    {
                        _logger.LogWarning("Trip with id {id} was not found", request.TripId);
                        return Result<DetailedTripDto>.Failure(DomainErrors.Trip.NotFound);
                    }

                    if (attempt > 1)
                        _logger.LogInformation("Retry attempt {Attempt}/{Max} for trip {TripId}", attempt, MaxRetryAttempts, request.TripId);

                    foreach (var change in request.Modification.Changes)
                    {
                        _logger.LogDebug("Handle for change type : {type}", change.Type.ToString());
                        switch (change.Type)
                        {
                            case "add_location":
                                var dayForAdd = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (dayForAdd == null)
                                {
                                    _logger.LogWarning("Day for add with trip id {tripid} and day index {dayindex} was not found", request.TripId, change.DayIndex);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.DayNotFound(change.DayIndex));
                                }
                                _logger.LogDebug("Day for add was found with id {tripdayid}", dayForAdd.Id);
                                var newLocation = Domain.Entities.TripLocation.Create(
                                    tripDayId: dayForAdd.Id,
                                    locationId: change.Location!.LocationId,
                                    orderIndex: change.Location.OrderIndex,
                                    startTime: _aiConvert.ConvertTimeOnlyToTimeSpan(change.Location.StartTime),
                                    endTime: _aiConvert.ConvertTimeOnlyToTimeSpan(change.Location.EndTime),
                                    note: change.Location.Description,
                                    transportMode: change.Location.TransportMode
                                );
                                // Explicitly mark as Added so EF generates INSERT (not UPDATE).
                                // TripLocation.Create() sets Id = Guid.NewGuid(); without AddAsync,
                                // EF sees a non-default PK in the tracked graph and treats it as
                                // an existing entity → UPDATE WHERE Id=@newGuid → 0 rows affected.
                                await _tripLocationRepository.AddAsync(newLocation);
                                if (!dayForAdd.TripLocations.Any(l => l.Id == newLocation.Id))
                                {
                                    dayForAdd.TripLocations.Add(newLocation);
                                }

                                if (change.Location.Alternatives != null)
                                {
                                    int priority = 1;
                                    foreach (var alt in change.Location.Alternatives)
                                    {
                                        var tripAlt = TripLocationAlternative.Create(
                                            tripLocationId: newLocation.Id,
                                            locationId: alt.LocationId,
                                            priority: priority++,
                                            reason: alt.Reason
                                        );
                                        await _tripLocationAlternativeRepository.AddAsync(tripAlt);
                                    }
                                }
                                affectedDayIndices.Add(change.DayIndex);
                                break;

                            case "remove_location":
                                var dayForRemove = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (dayForRemove == null)
                                {
                                    _logger.LogWarning("Day for remove with trip id {tripid} and day index {dayindex} was not found", request.TripId, change.DayIndex);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.DayNotFound(change.DayIndex));
                                }
                                var triplocationForRemove = dayForRemove.TripLocations.FirstOrDefault(l => l.LocationId.Equals(change.LocationId!.Value));
                                if (triplocationForRemove == null)
                                {
                                    _logger.LogWarning("Trip location with trip id {id} and day index {dayindex} and location id {locId} was not found", request.TripId, change.DayIndex, change.LocationId!.Value);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.LocationNotFoundForRemove);
                                }
                                dayForRemove.TripLocations.Remove(triplocationForRemove);
                                affectedDayIndices.Add(change.DayIndex);
                                break;

                            case "update_time":
                                var dayForUpdateTime = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (dayForUpdateTime == null)
                                {
                                    _logger.LogWarning("Day for update_time with trip id {tripid} and day index {dayindex} was not found", request.TripId, change.DayIndex);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.DayNotFound(change.DayIndex));
                                }
                                var matchingLocationsForTime = dayForUpdateTime.TripLocations.Where(l => l.LocationId.Equals(change.LocationId!.Value)).ToList();
                                if (!matchingLocationsForTime.Any())
                                {
                                    _logger.LogWarning("Trip location with trip id {id} and day index {dayindex} and location id {locId} was not found", request.TripId, change.DayIndex, change.LocationId!.Value);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.LocationNotFoundForUpdateTime);
                                }

                                Domain.Entities.TripLocation triplocationForUpdateTime;
                                if (matchingLocationsForTime.Count == 1)
                                {
                                    triplocationForUpdateTime = matchingLocationsForTime.First();
                                }
                                else
                                {
                                    if (change.StartTime.HasValue)
                                    {
                                        var targetTime = change.StartTime.Value.ToTimeSpan();
                                        triplocationForUpdateTime = matchingLocationsForTime.OrderBy(l => Math.Abs((l.StartTime ?? TimeSpan.Zero).TotalMinutes - targetTime.TotalMinutes)).First();
                                    }
                                    else if (change.EndTime.HasValue)
                                    {
                                        var targetTime = change.EndTime.Value.ToTimeSpan();
                                        triplocationForUpdateTime = matchingLocationsForTime.OrderBy(l => Math.Abs((l.EndTime ?? TimeSpan.Zero).TotalMinutes - targetTime.TotalMinutes)).First();
                                    }
                                    else
                                    {
                                        triplocationForUpdateTime = matchingLocationsForTime.First();
                                    }
                                }
                                if (change.StartTime.HasValue)
                                {
                                    triplocationForUpdateTime.StartTime = change.StartTime.Value.ToTimeSpan();
                                }
                                if (change.EndTime.HasValue)
                                {
                                    triplocationForUpdateTime.EndTime = change.EndTime.Value.ToTimeSpan();
                                }
                                affectedDayIndices.Add(change.DayIndex);
                                break;

                            case "update_location":
                                var dayForUpdate = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (dayForUpdate == null)
                                {
                                    _logger.LogWarning("Day for update_location with trip id {tripid} and day index {dayindex} was not found", request.TripId, change.DayIndex);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.DayNotFound(change.DayIndex));
                                }
                                var triplocationForUpdate = dayForUpdate.TripLocations.FirstOrDefault(l => l.LocationId.Equals(change.OldLocationId!.Value));
                                if (triplocationForUpdate == null)
                                {
                                    _logger.LogWarning("Trip location with trip id {id} and day index {dayindex} and location id {locId} was not found", request.TripId, change.DayIndex, change.OldLocationId!.Value);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.OldLocationNotFoundForUpdate);
                                }
                                dayForUpdate.TripLocations.Remove(triplocationForUpdate);

                                var updatedLocation = Domain.Entities.TripLocation.Create(
                                    tripDayId: dayForUpdate.Id,
                                    locationId: change.Location!.LocationId,
                                    orderIndex: change.Location.OrderIndex,
                                    startTime: _aiConvert.ConvertTimeOnlyToTimeSpan(change.Location.StartTime),
                                    endTime: _aiConvert.ConvertTimeOnlyToTimeSpan(change.Location.EndTime),
                                    note: change.Location.Description,
                                    transportMode: change.Location.TransportMode
                                );
                                await _tripLocationRepository.AddAsync(updatedLocation);
                                if (!dayForUpdate.TripLocations.Any(l => l.Id == updatedLocation.Id))
                                {
                                    dayForUpdate.TripLocations.Add(updatedLocation);
                                }

                                if (change.Location.Alternatives != null)
                                {
                                    int priority = 1;
                                    foreach (var alt in change.Location.Alternatives)
                                    {
                                        var tripAlt = TripLocationAlternative.Create(
                                            tripLocationId: updatedLocation.Id,
                                            locationId: alt.LocationId,
                                            priority: priority++,
                                            reason: alt.Reason
                                        );
                                        await _tripLocationAlternativeRepository.AddAsync(tripAlt);
                                    }
                                }
                                affectedDayIndices.Add(change.DayIndex);
                                break;

                            case "add_day":
                                var newDay = Domain.Entities.TripDay.Create(
                                    TripId: request.TripId,
                                    Tittle: change.Title ?? $"Ngày {change.DayIndex}",
                                    DayDate: change.Date.HasValue
                                        ? change.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                                        : (DateTime?)null,
                                    DayIndex: change.DayIndex
                                );
                                await _tripDayRepository.AddAsync(newDay);
                                if (change.Locations != null && change.Locations.Any())
                                {
                                    foreach (var loc in change.Locations.OrderBy(l => l.OrderIndex))
                                    {
                                        var dayLocation = Domain.Entities.TripLocation.Create(
                                            tripDayId: newDay.Id,
                                            locationId: loc.LocationId,
                                            orderIndex: loc.OrderIndex,
                                            startTime: _aiConvert.ConvertTimeOnlyToTimeSpan(loc.StartTime),
                                            endTime: _aiConvert.ConvertTimeOnlyToTimeSpan(loc.EndTime),
                                            note: loc.Description,
                                            transportMode: loc.TransportMode
                                        );
                                        newDay.TripLocations.Add(dayLocation);
                                        _logger.LogDebug("daylocation name {name} with start time {start} and end time {end}", loc.Name, dayLocation.StartTime.ToString(), dayLocation.EndTime.ToString());

                                        if (loc.Alternatives != null)
                                        {
                                            int priority = 1;
                                            foreach (var alt in loc.Alternatives)
                                            {
                                                var tripAlt = TripLocationAlternative.Create(
                                                    tripLocationId: dayLocation.Id,
                                                    locationId: alt.LocationId,
                                                    priority: priority++,
                                                    reason: alt.Reason
                                                );
                                                await _tripLocationAlternativeRepository.AddAsync(tripAlt);
                                            }
                                        }
                                    }
                                }
                                trip.TripDays.Add(newDay);
                                affectedDayIndices.Add(change.DayIndex);
                                break;

                            case "remove_day":
                                var tripdayForRemove = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (tripdayForRemove == null)
                                {
                                    _logger.LogWarning("Day for remove with trip id {tripid} and day index {dayindex} was not found", request.TripId, change.DayIndex);
                                    return Result<DetailedTripDto>.Failure(DomainErrors.ApplyModification.DayNotFound(change.DayIndex));
                                }
                                trip.TripDays.Remove(tripdayForRemove);
                                // A deleted day should also trigger a reorder logic if we were strictly relying on indices.
                                // Actually we handle day indices later in the hasDayChanges block.
                                break;

                            case "update_day_date":
                                var tripDayForUpdate = trip.TripDays.FirstOrDefault(d => d.DayIndex == change.DayIndex);
                                if (tripDayForUpdate == null)
                                {
                                    _logger.LogWarning(
                                        "TripDay with tripId {TripId} and dayIndex {DayIndex} was not found",
                                        request.TripId, change.DayIndex);
                                    break;
                                }
                                if (change.Date.HasValue)
                                {
                                    tripDayForUpdate.DayDate = change.Date.Value
                                        .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                                }
                                break;
                        }
                    }
                    var hasDayChanges = request.Modification.Changes
                        .Any(c => c.Type is "remove_day" or "add_day");
                    if (hasDayChanges)
                    {
                        int newDayIndex = 1;
                        Guid curGuid = Guid.Empty;
                        foreach (var day in trip.TripDays.OrderBy(d => d.DayDate ?? DateTime.MaxValue))
                        {
                            if (curGuid != Guid.Empty && curGuid == day.Id || day.DayIndex == 0 && day.Title.Equals("Ideas", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            day.DayIndex = newDayIndex;
                            _logger.LogDebug("Day {day} with index {index} and id {id}", day.DayDate.ToString(), day.DayIndex, day.Id);
                            newDayIndex++;
                            curGuid = day.Id;
                        }
                    }

                    foreach (var tripday in trip.TripDays)
                    {
                        int newLocIndex = 1;
                        foreach (var loc in tripday.TripLocations.OrderBy(l => l.StartTime ?? TimeSpan.MaxValue))
                        {
                            if (loc.OrderIndex != newLocIndex)
                            {
                                loc.OrderIndex = newLocIndex;
                            }
                            newLocIndex++;
                        }
                    }

                    // ====== OPTIMIZER INTEGRATION ======
                    //if (affectedDayIndices.Any())
                    //{
                    //    var tripPlanResponse = _mapper.Map<TripPlanResponse>(trip);
                    //    var tripLocationsForOptimizer = trip.TripDays.SelectMany(d => d.TripLocations.Select(tl => tl.Location)).Where(l => l != null).DistinctBy(l => l!.Id).ToList();
                        
                    //    var optimizedPlan = _optimizer.Optimize(tripPlanResponse, tripLocationsForOptimizer!);

                    //    foreach (var dayIndex in affectedDayIndices)
                    //    {
                    //        var optimizedDay = optimizedPlan.Days.FirstOrDefault(d => d.DayIndex == dayIndex);
                    //        var entityDay = trip.TripDays.FirstOrDefault(d => d.DayIndex == dayIndex);

                    //        if (optimizedDay != null && entityDay != null)
                    //        {
                    //            foreach (var optLoc in optimizedDay.Locations)
                    //            {
                    //                var entityLoc = entityDay.TripLocations.FirstOrDefault(l => l.LocationId == optLoc.LocationId);
                    //                if (entityLoc != null)
                    //                {
                    //                    entityLoc.OrderIndex = optLoc.OrderIndex;
                    //                    entityLoc.StartTime = _aiConvert.ConvertTimeOnlyToTimeSpan(optLoc.StartTime);
                    //                    entityLoc.EndTime = _aiConvert.ConvertTimeOnlyToTimeSpan(optLoc.EndTime);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    // ===================================
                    
                    var ordered = trip.TripDays.Where(d => d.DayIndex > 0 && d.DayDate.HasValue).OrderBy(d => d.DayIndex).ToList();
                    if (ordered.Any())
                    {
                        var firstDay = ordered.First();
                        _logger.LogDebug("First day : " + firstDay.DayDate.ToString());
                        var lastDay = ordered.Last();
                        _logger.LogDebug("Last day before update : " + lastDay.DayDate.ToString());

                        if (firstDay.DayDate.HasValue)
                        {
                            trip.StartDate = firstDay.DayDate.Value;
                            trip.UpdatedAt = DateTime.UtcNow;
                            trip.ModifiedDate = DateTime.UtcNow;
                            trip.ModifiedBy = userId.ToString();
                        }
                        if (lastDay.DayDate.HasValue)
                        {
                            trip.EndDate = lastDay.DayDate.Value.AddDays(1);
                            _logger.LogDebug("Last day after update : " + trip.EndDate.ToString());
                            trip.UpdatedAt = DateTime.UtcNow;
                            trip.ModifiedDate = DateTime.UtcNow;
                            trip.ModifiedBy = userId.ToString();
                        }
                    }

                    var aiMessage = ChatMessage.Create(
                        tripId: request.TripId,
                        senderId: userId,
                        content: request.Modification.Summary,
                        messageType: "trip_modification",
                        isAiMessage: true
                    );
                    await _chatMessageRepository.AddAsync(aiMessage);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<DetailedTripDto>.Success(_mapper.Map<DetailedTripDto>(trip));
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex,
                        "DbUpdateConcurrencyException on attempt {Attempt}/{Max} for trip {TripId}. Retrying...",
                        attempt, MaxRetryAttempts, request.TripId);
                    _unitOfWork.ClearChangeTracker();
                }
            }
            _logger.LogError("All {MaxRetries} retry attempts exhausted for trip {TripId}", MaxRetryAttempts, request.TripId);
            return Result<DetailedTripDto>.Failure(
                new Error("ApplyModification.ConcurrencyError","Đã có lỗi xảy ra khi lưu thay đổi sau nhiều lần thử. Vui lòng thử lại sau."));
        }
    }
}
