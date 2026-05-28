using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.Enums;
using LocationEntity = Vivu.Domain.Entities.Location;

namespace Vivu.Infrastructure.Services.AI
{
    public class TripItineraryOptimizer : ITripItineraryOptimizer
    {
        private readonly ILogger<TripItineraryOptimizer> _logger;

        public TripItineraryOptimizer(ILogger<TripItineraryOptimizer> logger)
        {
            _logger = logger;
        }

        public TripPlanResponse Optimize(TripPlanResponse tripPlan, List<LocationEntity> selectedLocations)
        {
            var locationLookup = selectedLocations.ToDictionary(l => l.Id);

            foreach (var day in tripPlan.Days)
            {
                day.Locations = ReorderDay(day.Locations, locationLookup);
            }

            return tripPlan;
        }

        private List<LocationPlanDto> ReorderDay(
            List<LocationPlanDto> locations,
            Dictionary<Guid, LocationEntity> lookup)
        {
            if (locations.Count <= 1) return locations;

            var anchors = new List<(int OriginalIndex, LocationPlanDto Dto)>();
            var flexibles = new List<(int OriginalIndex, LocationPlanDto Dto)>();

            for (var i = 0; i < locations.Count; i++)
            {
                var loc = locations[i];
                if (IsAnchor(loc, lookup))
                    anchors.Add((i, loc));
                else
                    flexibles.Add((i, loc));
            }

            if (!flexibles.Any())
                return UpdateOrderIndexes(locations);


            var result = new LocationPlanDto[locations.Count];

            foreach (var (origIdx, dto) in anchors)
                result[origIdx] = dto;

            var anchorPositions = anchors.Select(a => a.OriginalIndex).ToList();


            var segments = BuildSegments(anchorPositions, locations.Count, flexibles);

            foreach (var segment in segments)
            {
                if (!segment.Slots.Any() || !segment.Flexibles.Any()) continue;

                var startRef = segment.StartAnchorIndex >= 0
                    ? GetCoords(locations[segment.StartAnchorIndex], lookup)
                    : GetCoords(segment.Flexibles.First().Dto, lookup);

                var ordered = NearestNeighborOrder(segment.Flexibles.Select(f => f.Dto).ToList(), startRef, lookup);

                for (var i = 0; i < segment.Slots.Count && i < ordered.Count; i++)
                {
                    var slotIndex = segment.Slots[i];
                    var original = locations[slotIndex];
                    var newContent = ordered[i];

                    result[slotIndex] = new LocationPlanDto
                    {
                        LocationId    = newContent.LocationId,
                        Name          = newContent.Name,
                        Description   = newContent.Description,
                        TransportMode = newContent.TransportMode,
                        OrderIndex    = newContent.OrderIndex,
                        StartTime     = original.StartTime,
                        EndTime       = original.EndTime,
                        // Alternatives belong to the primary — carry them along after reorder
                        Alternatives  = newContent.Alternatives
                    };
                }
            }

            var remaining = flexibles.Select(f => f.Dto).ToList();
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] == null && remaining.Any())
                {
                    result[i] = remaining.First();
                    remaining.RemoveAt(0);
                }
            }

            return UpdateOrderIndexes(result.Where(r => r != null).ToList()!);
        }

        private List<LocationPlanDto> NearestNeighborOrder(
            List<LocationPlanDto> locations,
            (double lat, double lng) startCoords,
            Dictionary<Guid, LocationEntity> lookup)
        {
            if (locations.Count <= 1) return locations;

            var unvisited = locations.ToList();
            var ordered = new List<LocationPlanDto>();
            var currentCoords = startCoords;

            while (unvisited.Any())
            {
                var nearest = unvisited
                    .OrderBy(loc => HaversineDistance(currentCoords, GetCoords(loc, lookup)))
                    .First();

                ordered.Add(nearest);
                currentCoords = GetCoords(nearest, lookup);
                unvisited.Remove(nearest);
            }

            return ordered;
        }

        private bool IsAnchor(LocationPlanDto loc, Dictionary<Guid, LocationEntity> lookup)
        {
            if (!lookup.TryGetValue(loc.LocationId, out var entity))
                return false;

            var type = entity.Category?.CategoryType ?? LocationCategoryType.Other;
            return type == LocationCategoryType.Food || type == LocationCategoryType.Accommodation;
        }

        private (double lat, double lng) GetCoords(LocationPlanDto loc, Dictionary<Guid, LocationEntity> lookup)
        {
            if (lookup.TryGetValue(loc.LocationId, out var entity)
                && entity.Latitude.HasValue && entity.Longitude.HasValue)
                return (entity.Latitude.Value, entity.Longitude.Value);

            return (0, 0);
        }

        private static double HaversineDistance((double lat, double lng) a, (double lat, double lng) b)
        {
            const double R = 6371;
            var dLat = ToRad(b.lat - a.lat);
            var dLng = ToRad(b.lng - a.lng);
            var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(a.lat)) * Math.Cos(ToRad(b.lat))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

        private static List<LocationPlanDto> UpdateOrderIndexes(List<LocationPlanDto> locations)
        {
            for (var i = 0; i < locations.Count; i++)
                locations[i].OrderIndex = i + 1;
            return locations;
        }

        private static List<Segment> BuildSegments(
            List<int> anchorPositions,
            int totalCount,
            List<(int OriginalIndex, LocationPlanDto Dto)> flexibles)
        {
            var segments = new List<Segment>();

            // Boundaries: -1 (before first anchor) and each anchor position
            var boundaries = new List<int> { -1 };
            boundaries.AddRange(anchorPositions);
            boundaries.Add(totalCount); // sentinel end

            for (var i = 0; i < boundaries.Count - 1; i++)
            {
                var start = boundaries[i];
                var end = boundaries[i + 1];

                var freeSlots = Enumerable.Range(start + 1, end - start - 1).ToList();
                var segFlexibles = flexibles
                    .Where(f => f.OriginalIndex > start && f.OriginalIndex < end)
                    .ToList();

                if (freeSlots.Any() && segFlexibles.Any())
                    segments.Add(new Segment(start, freeSlots, segFlexibles));
            }

            return segments;
        }

        private record Segment(
            int StartAnchorIndex,
            List<int> Slots,
            List<(int OriginalIndex, LocationPlanDto Dto)> Flexibles);
    }
}
