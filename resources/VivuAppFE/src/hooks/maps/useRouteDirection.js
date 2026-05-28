import { useMemo } from "react";
import useSWR from "swr";
import goongApi from "../../api/goongApi";

// Helper for rate-limited batch execution
const batchThrottle = async (taskFns, batchSize = 3, delayMs = 300) => {
  const results = [];
  for (let i = 0; i < taskFns.length; i += batchSize) {
    const batch = taskFns.slice(i, i + batchSize);
    // Vercel Rule: async-parallel - Parallel within batch
    const batchResults = await Promise.allSettled(batch.map((fn) => fn()));
    results.push(...batchResults);

    // Vercel Rule: async-error-handling - Wait between batches if more exist
    if (i + batchSize < taskFns.length) {
      await new Promise((r) => setTimeout(r, delayMs));
    }
  }
  return results;
};

// SWR Fetcher for multi-leg directions
const fetchRouteDirections = async (locations) => {
  if (!locations || locations.length < 2) return null;

  // Vercel Rule: js-early-exit - Validations
  const validLocations = locations.filter(
    (loc) => loc.latitude && loc.longitude,
  );
  if (validLocations.length < 2) return null;

  // Build origin/destination tasks (lazy)
  const tasks = [];
  for (let i = 0; i < validLocations.length - 1; i++) {
    const origin = `${validLocations[i].latitude},${validLocations[i].longitude}`;
    const dest = `${validLocations[i + 1].latitude},${validLocations[i + 1].longitude}`;
    tasks.push(() => goongApi.direction(origin, dest));
  }

  // Execute in batches to respect Goong API rate limits (5 req/s)
  const results = await batchThrottle(tasks, 3, 300);

  // Vercel Rule: js-combine-iterations - Process everything in one pass
  let totalDistanceValue = 0;
  let totalDurationValue = 0;
  const polylines = [];
  const legs = [];

  results.forEach((result, index) => {
    // Vercel Rule: async-error-handling - Tolerate individual leg failures
    if (result.status === "rejected") {
      console.warn(`Route leg ${index} failed:`, result.reason);
      return;
    }

    const data = result.value;
    if (data?.routes?.[0]) {
      const route = data.routes[0];

      // Extract encoded polyline
      if (route.overview_polyline?.points) {
        polylines.push(route.overview_polyline.points);
      }

      // Aggregate distance and duration
      if (route.legs?.[0]) {
        const leg = route.legs[0];
        totalDistanceValue += leg.distance?.value || 0;
        totalDurationValue += leg.duration?.value || 0;
        legs.push(leg);
      }
    }
  });

  return {
    totalDistance: totalDistanceValue, // in meters
    totalDuration: totalDurationValue, // in seconds
    polylines,
    legs,
  };
};

export function useRouteDirection(locations) {
  // Vercel Rule: rerender-lazy-state-init / rerender-dependencies
  // Stable key based on coordinates to leverage SWR deduplication
  const cacheKey = useMemo(() => {
    if (!locations || locations.length < 2) return null;
    const coordsHash = locations
      .filter((l) => l.latitude && l.longitude)
      .map((l) => `${l.latitude},${l.longitude}`)
      .join("|");

    if (coordsHash.split("|").length < 2) return null;
    return ["goong-direction", coordsHash];
  }, [locations]);

  // Vercel Rule: client-swr-dedup
  const { data, isLoading, error } = useSWR(
    cacheKey,
    () => fetchRouteDirections(locations),
    {
      revalidateOnFocus: false,
      revalidateIfStale: false,
      dedupingInterval: 60000, // aggressive caching for identical routes (1 minute)
    },
  );

  return {
    routeData: data,
    isLoading,
    error,
  };
}
