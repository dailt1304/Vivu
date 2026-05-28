import useSWR, { SWRConfig } from "swr";
import axiosClient from "../api/axiosClient";

/**
 * Global fetcher for SWR
 * Uses axiosClient which already handles auth tokens
 */
const fetcher = async (url) => {
  const response = await axiosClient.get(url);
  if (response.success) {
    return response.data;
  }
  throw new Error(response.message || "Failed to fetch");
};

/**
 * SWR Provider - wraps app with global SWR configuration
 * Features:
 * - Deduplication: Multiple components requesting same data = 1 request
 * - Revalidation on focus: Auto-refresh when tab is focused
 * - Error retry with exponential backoff
 */
export function SwrProvider({ children }) {
  return (
    <SWRConfig
      value={{
        fetcher,
        revalidateOnFocus: true,
        revalidateOnReconnect: true,
        dedupingInterval: 2000, // Dedup requests within 2 seconds
        errorRetryCount: 3,
        errorRetryInterval: 1000,
      }}
    >
      {children}
    </SWRConfig>
  );
}

/**
 * useTrip - SWR-based hook for fetching a single trip
 *
 * Benefits over useTripDetails:
 * - Automatic caching & deduplication
 * - Background revalidation
 * - Optimistic UI updates via mutate
 *
 * @param {string} tripId - Trip ID to fetch
 * @returns {{ trip, isLoading, error, mutate }}
 */
export function useTrip(tripId) {
  const { data, error, isLoading, mutate } = useSWR(
    tripId ? `/trips/${tripId}` : null,
  );

  return {
    trip: data || null,
    isLoading,
    error: error?.message || null,
    mutate, // Call mutate() to refresh, or mutate(newData) for optimistic update
  };
}

/**
 * useUserTripsSWR - SWR-based hook for fetching user's trips
 *
 * @param {string} userId - User ID
 * @returns {{ trips, isLoading, error, mutate }}
 */
export function useUserTripsSWR(userId) {
  const { data, error, isLoading, mutate } = useSWR(
    userId ? `/trips/user/${userId}` : null,
  );

  return {
    trips: data?.trips?.items || [],
    total: data?.trips?.totalCount || 0,
    isLoading,
    error: error?.message || null,
    mutate,
  };
}

export { fetcher };
