using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Domain.Enums;
using LocationEntity = Vivu.Domain.Entities.Location;

namespace Vivu.Infrastructure.Services.AI
{
    public class LocationZoneService : ILocationZoneService
    {
        private const int GridSize = 3;
        private const int AttractionsPerZone = 18;

        private const int FoodPerZone = 20;

        private const int ShoppingEntertainmentPerZone = 10;

        private const int AccommodationTotal = 5;

        private readonly ILogger<LocationZoneService> _logger;

        public LocationZoneService(ILogger<LocationZoneService> logger)
        {
            _logger = logger;
        }

        public List<LocationEntity> SelectLocationsForTrip(
            List<LocationEntity> allLocations,
            int daysCount,
            List<string> preferences,
            TripConstraints? constraints = null,
            UserPersonalizationContext? personalization = null)
        {
            if (constraints?.MustExcludeTypes.Any() == true)
            {
                var before = allLocations.Count;
                allLocations = allLocations.Where(l =>
                    !constraints.MustExcludeTypes.Any(exclude =>
                        l.Name?.Contains(exclude, StringComparison.OrdinalIgnoreCase) == true ||
                        l.Category?.Name?.Contains(exclude, StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

                _logger.LogDebug("MustExcludeTypes filtered {Removed} locations ({Before} → {After})",
                    before - allLocations.Count, before, allLocations.Count);
            }

            if (personalization?.AllVisitedLocationIds.Any() == true)
            {
                var before = allLocations.Count;
                var visitedIds = personalization.AllVisitedLocationIds.ToHashSet();

                allLocations = allLocations.Where(l => !visitedIds.Contains(l.Id)).ToList();

                _logger.LogDebug("Personalization filtered {Removed} visited locations ({Before} → {After})",
                    before - allLocations.Count, before, allLocations.Count);
            }

            var locationsWithCoords = allLocations
                .Where(l => l.Latitude.HasValue && l.Longitude.HasValue)
                .ToList();

            if (!locationsWithCoords.Any())
            {
                _logger.LogWarning("No locations with coordinates, falling back to top-rated");
                return allLocations
                    .OrderByDescending(l => Score(l, preferences, personalization))
                    .Take(daysCount * 15)
                    .ToList();
            }

            // Step 1: Bounding box
            var minLat = locationsWithCoords.Min(l => l.Latitude!.Value);
            var maxLat = locationsWithCoords.Max(l => l.Latitude!.Value);
            var minLng = locationsWithCoords.Min(l => l.Longitude!.Value);
            var maxLng = locationsWithCoords.Max(l => l.Longitude!.Value);

            var latStep = Math.Max((maxLat - minLat) / GridSize, 1e-9);
            var lngStep = Math.Max((maxLng - minLng) / GridSize, 1e-9);

            // Step 2: Assign zone
            var zoneMap = new Dictionary<(int row, int col), List<LocationEntity>>();
            foreach (var loc in locationsWithCoords)
            {
                var row = Math.Min((int)((loc.Latitude!.Value - minLat) / latStep), GridSize - 1);
                var col = Math.Min((int)((loc.Longitude!.Value - minLng) / lngStep), GridSize - 1);
                var key = (row, col);
                if (!zoneMap.ContainsKey(key))
                    zoneMap[key] = new List<LocationEntity>();
                zoneMap[key].Add(loc);
            }

            // Step 3: Score zones by best attraction (preferences-aware)
            var zoneScores = zoneMap
                .Where(z => z.Value.Any(l => l.Category?.CategoryType == LocationCategoryType.Attraction))
                .Select(z => new
                {
                    Zone = z.Key,
                    Score = z.Value
                        .Where(l => l.Category?.CategoryType == LocationCategoryType.Attraction)
                        .Max(l => Score(l, preferences, personalization))
                })
                .OrderByDescending(z => z.Score)
                .ToList();

            // Step 4: Pick top N distinct zones; if daysCount > zones, boost attraction quota for top zones
            var distinctZoneCount = Math.Min(daysCount, zoneScores.Count);
            var selectedZones = zoneScores
                .Take(distinctZoneCount)
                .Select(z => z.Zone)
                .ToList();

            if (!selectedZones.Any())
            {
                _logger.LogWarning("No zones with attractions found, falling back to top-rated");
                return allLocations
                    .OrderByDescending(l => Score(l, preferences, personalization))
                    .Take(daysCount * 15)
                    .ToList();
            }

            // Extra days beyond available zones: boost top zones' attraction quota
            var extraDays = daysCount - distinctZoneCount;
            var zoneVisits = selectedZones.ToDictionary(z => z, _ => 1);

            for (var i = 0; i < extraDays; i++)
            {
                zoneVisits[zoneScores[i % distinctZoneCount].Zone]++;
            }

            _logger.LogDebug(
                "Selected {Zones} zones for {Days}-day trip (extraDays={Extra})",
                selectedZones.Count, daysCount, extraDays);

            var result = new List<LocationEntity>();
            var addedIds = new HashSet<Guid>();

            foreach (var zone in selectedZones)
            {
                var zoneLocations = zoneMap[zone];
                var visits = zoneVisits[zone];

                //use preference-aware score for attraction ranking
                var anchorAttractions = zoneLocations
                    .Where(l => l.Category?.CategoryType == LocationCategoryType.Attraction)
                    .OrderByDescending(l => Score(l, preferences, personalization))
                    .Take(AttractionsPerZone * visits);

                //budget removed from food sorting — Score() is already reliable
                var food = GetFromZoneWithAdjacent(
                    zone, zoneMap,
                    l => l.Category?.CategoryType == LocationCategoryType.Food,
                    FoodPerZone * visits,
                    personalization);

                var shoppingEnt = zoneLocations
                    .Where(l => l.Category?.CategoryType == LocationCategoryType.Shopping
                             || l.Category?.CategoryType == LocationCategoryType.Entertainment)
                    .OrderByDescending(l => Score(l, preferences, personalization))
                    .Take(ShoppingEntertainmentPerZone * visits);

                foreach (var loc in anchorAttractions.Concat(food).Concat(shoppingEnt))
                {
                    if (addedIds.Add(loc.Id))
                        result.Add(loc);
                }
            }

            //Add top accommodations city-wide (anchors for TripItineraryOptimizer)
            var topAccommodations = allLocations
                .Where(l => l.Category?.CategoryType == LocationCategoryType.Accommodation)
                .OrderByDescending(l => Score(l, preferences, personalization))
                .Take(AccommodationTotal);

            foreach (var loc in topAccommodations)
            {
                if (addedIds.Add(loc.Id))
                    result.Add(loc);
            }

            if (constraints?.MustIncludeTypes.Any() == true)
            {
                foreach (var typeKeyword in constraints.MustIncludeTypes)
                {
                    var alreadyHas = result.Any(l =>
                        l.Name?.Contains(typeKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                        l.Category?.Name?.Contains(typeKeyword, StringComparison.OrdinalIgnoreCase) == true);

                    if (!alreadyHas)
                    {
                        var missing = allLocations
                            .Where(l =>
                                l.Name?.Contains(typeKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                                l.Category?.Name?.Contains(typeKeyword, StringComparison.OrdinalIgnoreCase) == true)
                            .OrderByDescending(l => Score(l, preferences, personalization))
                            .Take(5);

                        foreach (var loc in missing)
                            if (addedIds.Add(loc.Id))
                                result.Add(loc);

                        _logger.LogDebug("Force-added locations for mustIncludeType '{Type}'", typeKeyword);
                    }
                }
            }

            if (constraints?.MustIncludeKeywords.Any() == true)
            {
                foreach (var keyword in constraints.MustIncludeKeywords)
                {
                    var match = allLocations
                     .Where(l => IsFuzzyMatch(l.Name, keyword))
                     .OrderByDescending(l => Score(l, preferences, personalization))
                     .FirstOrDefault();

                    _logger.LogDebug("For list force add name {name} have taken {match}", allLocations.Where(l => IsFuzzyMatch(l.Name,keyword)
                    ).OrderByDescending(l => Score(l,preferences, personalization)).Select(l => l.Name).ToString(), match == null ?"" : match.Name);

                    if (match == null) continue;

                    if (addedIds.Add(match.Id))
                    {
                        result.Add(match);
                        _logger.LogDebug(
                            "Force-added keyword location '{Name}' (keyword: '{Keyword}')",
                            match.Name, keyword);
                    }

                    if (match.Latitude.HasValue && match.Longitude.HasValue)
                    {
                        var matchRow = Math.Min(
                            (int)((match.Latitude.Value - minLat) / latStep), GridSize - 1);
                        var matchCol = Math.Min(
                            (int)((match.Longitude.Value - minLng) / lngStep), GridSize - 1);
                        var matchZone = (matchRow, matchCol);

                        if (!selectedZones.Contains(matchZone) && zoneMap.ContainsKey(matchZone))
                        {
                            var zoneCompanions = zoneMap[matchZone]
                                .OrderByDescending(l => Score(l, preferences, personalization))
                                .Take(15); 

                            foreach (var companion in zoneCompanions)
                                if (addedIds.Add(companion.Id))
                                    result.Add(companion);

                            _logger.LogDebug(
                                "Added companion zone ({Row},{Col}) for keyword '{Keyword}'",
                                matchRow, matchCol, keyword);
                        }
                    }
                }
            }

            _logger.LogDebug("Pre-selected {Count} locations for AI (vs {Total} total)", result.Count, allLocations.Count);
            return result;
        }
        public List<LocationEntity> SelectLocationsForModify(
            List<LocationEntity> allLocations,
            List<LocationEntity> currentTripLocations,
            string userRequest,
            UserPersonalizationContext? personalization = null,
            TripConstraints? constraints = null)
        {
            var result = new List<LocationEntity>();
            var addedIds = new HashSet<Guid>();

            if (!string.IsNullOrWhiteSpace(userRequest))
            {
                var normalizedRequest = Domain.Entities.City.RemoveDiacritics(userRequest).ToLowerInvariant();
                var forcedLocations = allLocations.Where(l =>
                {
                    if (string.IsNullOrWhiteSpace(l.Name)) return false;
                    var normalizedName = Domain.Entities.City.RemoveDiacritics(l.Name).ToLowerInvariant();
                    return normalizedRequest.Contains(normalizedName) || normalizedName.Contains(normalizedRequest);
                }).ToList();

                foreach (var loc in forcedLocations)
                {
                    if (addedIds.Add(loc.Id)) result.Add(loc);
                }
            }

            // 2. Filter Visited
            if (personalization?.AllVisitedLocationIds.Any() == true)
            {
                var before = allLocations.Count;

                var visitedIds = personalization.AllVisitedLocationIds.ToHashSet();

                allLocations = allLocations.Where(l => !visitedIds.Contains(l.Id)).ToList();

                _logger.LogDebug("Personalization filtered {Removed} visited locations ({Before} → {After})",
                    before - allLocations.Count, before, allLocations.Count);
            }

            // 2.5 Filter Constraints Exclusions
            if (constraints != null)
            {
                if (constraints.MustExcludeTypes.Any())
                {
                    var excludedCats = constraints.MustExcludeTypes.Select(c => c.ToLowerInvariant()).ToHashSet();
                    allLocations = allLocations.Where(l => l.Category == null || !excludedCats.Contains(l.Category.Name?.ToLowerInvariant() ?? "")).ToList();
                }

                //if (constraints.FreeformRules.Any())
                //{
                //}
            }

            // 3. Find Zones of existing trip locations
            var locationsWithCoords = allLocations.Where(l => l.Latitude.HasValue && l.Longitude.HasValue).ToList();
            if (!locationsWithCoords.Any())
            {
                var topFallback = allLocations.OrderByDescending(l => Score(l, null, personalization)).Take(80);
                foreach (var loc in topFallback)
                    if (addedIds.Add(loc.Id)) result.Add(loc);
                return result;
            }

            var minLat = locationsWithCoords.Min(l => l.Latitude!.Value);
            var maxLat = locationsWithCoords.Max(l => l.Latitude!.Value);
            var minLng = locationsWithCoords.Min(l => l.Longitude!.Value);
            var maxLng = locationsWithCoords.Max(l => l.Longitude!.Value);

            var latStep = Math.Max((maxLat - minLat) / GridSize, 1e-9);
            var lngStep = Math.Max((maxLng - minLng) / GridSize, 1e-9);

            var zoneMap = new Dictionary<(int row, int col), List<LocationEntity>>();
            foreach (var loc in locationsWithCoords)
            {
                var row = Math.Min((int)((loc.Latitude!.Value - minLat) / latStep), GridSize - 1);
                var col = Math.Min((int)((loc.Longitude!.Value - minLng) / lngStep), GridSize - 1);
                var key = (row, col);
                if (!zoneMap.ContainsKey(key)) zoneMap[key] = new List<LocationEntity>();
                zoneMap[key].Add(loc);
            }

            var affectedZones = new HashSet<(int row, int col)>();
            foreach (var loc in currentTripLocations.Where(l => l.Latitude.HasValue && l.Longitude.HasValue))
            {
                var row = Math.Min((int)((loc.Latitude!.Value - minLat) / latStep), GridSize - 1);
                var col = Math.Min((int)((loc.Longitude!.Value - minLng) / lngStep), GridSize - 1);
                affectedZones.Add((row, col));
            }

            // If trip has no coords, select top 2 zones
            if (!affectedZones.Any())
            {
                var topZones = zoneMap.OrderByDescending(z => z.Value.Count).Take(2).Select(z => z.Key);
                foreach(var z in topZones) affectedZones.Add(z);
            }

            // 4. Pick top spots from these zones
            foreach (var zone in affectedZones)
            {
                var topFood = GetFromZoneWithAdjacent(zone, zoneMap, l => l.Category?.CategoryType == LocationCategoryType.Food, 15, personalization);
                var topAttractions = GetFromZoneWithAdjacent(zone, zoneMap, l => l.Category?.CategoryType == LocationCategoryType.Attraction, 15, personalization);
                var topShopping = GetFromZoneWithAdjacent(zone, zoneMap, l => l.Category?.CategoryType == LocationCategoryType.Shopping || l.Category?.CategoryType == LocationCategoryType.Entertainment, 10, personalization);

                foreach (var loc in topFood.Concat(topAttractions).Concat(topShopping))
                    if (addedIds.Add(loc.Id)) result.Add(loc);
            }

            // Guarantee top accommodations overall as fallbacks
            var topAccommodations = allLocations
                .Where(l => l.Category?.CategoryType == LocationCategoryType.Accommodation)
                .OrderByDescending(l => Score(l, null, personalization))
                .Take(5);
            foreach (var loc in topAccommodations)
                if (addedIds.Add(loc.Id)) result.Add(loc);

            // Limit to max 80 locations total to prevent token explosion
            if (result.Count > 80)
            {
                // Prioritize forced, then by score
                var forcedCount = result.Count(l => !string.IsNullOrWhiteSpace(userRequest) && Domain.Entities.City.RemoveDiacritics(userRequest).ToLowerInvariant().Contains(Domain.Entities.City.RemoveDiacritics(l.Name ?? "").ToLowerInvariant()));
                var remainingSlots = 80 - forcedCount;
                if (remainingSlots < 0) remainingSlots = 0;

                var forced = result.Where(l => !string.IsNullOrWhiteSpace(userRequest) && Domain.Entities.City.RemoveDiacritics(userRequest).ToLowerInvariant().Contains(Domain.Entities.City.RemoveDiacritics(l.Name ?? "").ToLowerInvariant())).ToList();
                var others = result.Except(forced).OrderByDescending(l => Score(l, null, personalization)).Take(remainingSlots);
                
                result = forced.Concat(others).ToList();
            }

            return result;
        }

        private static List<LocationEntity> GetFromZoneWithAdjacent(
            (int row, int col) zone,
            Dictionary<(int row, int col), List<LocationEntity>> zoneMap,
            Func<LocationEntity, bool> filter,
            int targetCount,
            UserPersonalizationContext? personalization = null)
        {
            var candidateZones = GetZoneWithAdjacent(zone)
                .Where(z => zoneMap.ContainsKey(z));

            return candidateZones
                .SelectMany(z => zoneMap[z])
                .Where(filter)
                .DistinctBy(l => l.Id)
                .OrderByDescending(l => Score(l, null, personalization))
                .Take(targetCount)
                .ToList();
        }

        private static IEnumerable<(int row, int col)> GetZoneWithAdjacent((int row, int col) zone)
        {
            for (var dr = -1; dr <= 1; dr++)
            for (var dc = -1; dc <= 1; dc++)
            {
                var r = zone.row + dr;
                var c = zone.col + dc;
                if (r >= 0 && r < GridSize && c >= 0 && c < GridSize)
                    yield return (r, c);
            }
        }

        private static double Score(LocationEntity l, List<string>? preferences, UserPersonalizationContext? personalization)
        {
            var base_ = (double)l.RatingAverage * Math.Log(l.RatingCount + 1);

            if (personalization != null && l.Id != Guid.Empty && l.RatingAverage > 0)
            {
                double variation = Math.Abs((l.Id.GetHashCode() ^ personalization.PersonalityEntropySeed) % 100) / 100.0;
                base_ += base_ * 0.2 * variation;
            }
            if (personalization != null && !string.IsNullOrWhiteSpace(l.Category?.Name))
            {
                var catName = l.Category.Name;
                double groupModifier = 1.0;

                switch (personalization.GroupComposition)
                {
                    case GroupCompositionType.FamilyWithChildren:
                        if (catName.Contains("Vui chơi", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Công viên", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Thiên nhiên", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 1.3;

                        else if (catName.Contains("Về đêm", StringComparison.OrdinalIgnoreCase) ||
                                 catName.Contains("Tâm linh", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 0.5;
                        break;

                    case GroupCompositionType.FriendsGroup:
                        if (catName.Contains("Về đêm", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Đồ nướng", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Hải sản", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Cà phê", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Đường phố", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Giải trí", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 1.3;

                        else if (catName.Contains("Tâm linh", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 0.3;
                        break;

                    case GroupCompositionType.Senior:
                        if (catName.Contains("Tâm linh", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Văn hóa", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Chay", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Sinh thái", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 1.4;

                        else if (catName.Contains("Về đêm", StringComparison.OrdinalIgnoreCase) ||
                                 catName.Contains("Giải trí", StringComparison.OrdinalIgnoreCase) ||
                                 catName.Contains("Đường phố", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 0.4;
                        break;

                    case GroupCompositionType.Couple:
                        if (catName.Contains("Cà phê", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Sinh thái", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Về đêm", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Cao cấp", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 1.2;
                        break;

                    case GroupCompositionType.Solo:
                        if (catName.Contains("Cà phê", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Đường phố", StringComparison.OrdinalIgnoreCase) ||
                            catName.Contains("Văn hóa", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 1.2;
                        else if (catName.Contains("Hải sản", StringComparison.OrdinalIgnoreCase) ||
                                 catName.Contains("Đồ nướng", StringComparison.OrdinalIgnoreCase))
                            groupModifier = 0.7;
                        break;
                }

                base_ *= groupModifier;
            }
            if (preferences == null || preferences.Count == 0) return base_;

            var boosted = false;
            foreach (var pref in preferences)
            {
                var nameMatches = l.Name?.IndexOf(pref, StringComparison.OrdinalIgnoreCase) >= 0;
                var categoryMatches = l.Category?.Name?.IndexOf(pref, StringComparison.OrdinalIgnoreCase) >= 0;
                if (nameMatches || categoryMatches)
                {
                    boosted = true;
                    break;
                }
            }

            return boosted ? base_ * 1.5 : base_;
        }

        private static bool IsFuzzyMatch(string dbLocationName, string searchKeyword)
        {
            if (string.IsNullOrWhiteSpace(dbLocationName) || string.IsNullOrWhiteSpace(searchKeyword))
                return false;

            if (dbLocationName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase))
                return true;

            var normalizedDb = Vivu.Domain.Entities.City.RemoveDiacritics(dbLocationName).ToLowerInvariant();
            var normalizedKw = Vivu.Domain.Entities.City.RemoveDiacritics(searchKeyword).ToLowerInvariant();

            if (normalizedDb.Contains(normalizedKw)) return true;

            // (nha, ong, kich, coffe) vs (ong, kich, ca, phe)
            var dbTokens = normalizedDb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var kwTokens = normalizedKw.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Đếm xem có bao nhiêu từ khóa của user xuất hiện trong tên của DB
            int matchCount = 0;
            foreach (var kw in kwTokens)
            {
                if (dbTokens.Contains(kw)) matchCount++;
            }

            // Nếu tỷ lệ trùng lặp >= 50% số từ của keyword, coi như Match!
            // Ví dụ: keyword có 4 từ (ong, kich, ca, phe). Trúng 2 từ (ong, kich) -> 2/4 = 50% -> Khớp!
            return ((double)matchCount / kwTokens.Length) >= 0.5;
        }
    }
}
