import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import locationApi from "../../api/locationApi";
import locationCategoryApi from "../../api/locationCategoryApi";

// Hook for filtering locations
export function useLocationsByFilter(params) {
  // SWR key array allows caching and tracking
  // Stringify params to ensure stable key reference even if the object identity changes on every render
  const key = params ? ["locations-filter", JSON.stringify(params)] : null;

  return useSWR(key, () =>
    locationApi.getByFilter(params).then((res) => res.data),
    {
      revalidateOnFocus: false,
      revalidateIfStale: false,
      dedupingInterval: 30000,
    }
  );
}

// Hook for location detail
export function useLocationById(id) {
  return useSWR(id ? `location-${id}` : null, () =>
    locationApi.getById(id).then((res) => res.data),
    {
      revalidateOnFocus: false,
      revalidateIfStale: false,
      dedupingInterval: 60000,
    },
  );
}

// Hook for location categories
export function useLocationCategories() {
  return useSWR(
    "location-categories-all",
    () => locationCategoryApi.getAll({ pageSize: 100, pageNumber: 1 }).then((res) => res.data),
    {
      revalidateOnFocus: false, // Categories rarely change
      revalidateIfStale: false,
    },
  );
}

// Hook for user's submitted locations
export function useMySubmissions(params) {
  // Stringify params to ensure stable key reference
  const key = params ? ["my-submissions", JSON.stringify(params)] : null;
  return useSWR(key, () =>
    locationApi.getMySubmissions(params).then((res) => res.data),
  );
}

// Mutation hook for submitting a new location
export function useSubmitLocation() {
  return useSWRMutation("submit-location", (_, { arg: formData }) =>
    locationApi.submit(formData).then((res) => res.data),
  );
}

// Mutation hook for reporting a location
export function useReportLocation() {
  return useSWRMutation("report-location", (_, { arg: formData }) =>
    locationApi.report(formData).then((res) => res.data),
  );
}

// Mutation hook for suggesting updates to a submitted location
export function useSuggestUpdateLocation() {
  return useSWRMutation("suggest-update-location", (_, { arg }) =>
    locationApi
      .suggestUpdate(arg.id, arg.formData)
      .then((res) => res.data),
  );
}

// Hook for popular locations (Landing Page + Explore Page)
export function usePopularLocations(
  pageNumber = 1,
  pageSize = 6,
  enabled = true,
  cityId = null,
  categoryId = null,
) {
  return useSWR(
    enabled ? ["popular-locations", pageNumber, pageSize, cityId, categoryId] : null,
    () =>
      locationApi
        .getPopular({
          pageNumber,
          pageSize,
          ...(cityId && { cityId }),
          ...(categoryId && { categoryId }),
        })
        .then((res) => res.data),
    {
      revalidateOnFocus: false, // Popular locations rarely change
      revalidateIfStale: false,
    },
  );
}

/**
 * Hook for nearby locations (Explore Page)
 * Vercel: js-early-exit — SWR key = null khi chưa có position → không fetch
 * Vercel: client-swr-dedup — cache theo lat/lng/radius key
 */
export function useNearbyLocations(
  position,
  radiusInMeters = 10000,
  limit = 20,
  categoryId = null,
) {
  const key = position
    ? [
        "nearby-locations",
        position.latitude,
        position.longitude,
        radiusInMeters,
        categoryId,
      ]
    : null;

  return useSWR(key, () =>
    locationApi
      .getNearby({
        latitude: position.latitude,
        longitude: position.longitude,
        radiusInMeters,
        isVerifiedOnly: true,
        limit,
        categoryId: categoryId || undefined,
      })
      .then((res) => res.data),
  );
}

export function useTrendingDestinations(pageSize = 6, locationsPerCity = 1) {
  return useSWR(
    ["trending-destinations", pageSize, locationsPerCity],
    () =>
      locationApi
        .getTrending({
          pageNumber: 1,
          pageSize,
          locationsPerCity,
        })
        .then((res) => res.data),
    {
      revalidateOnFocus: false,
      revalidateIfStale: false,
    },
  );
}
