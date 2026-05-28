import useSWR from "swr";
import tripApi from "../../api/tripApi";
import userApi from "../../api/userApi";

/**
 * Hook to fetch trips owned by a specific user
 */
export function useUserTrips(userId, params = { pageNumber: 1, pageSize: 20 }) {
  const { data, error, isLoading, mutate } = useSWR(
    userId ? ["user-trips", userId, params] : null,
    () => tripApi.getUserTrips(userId, params).then((res) => res.data),
  );

  return {
    trips: data?.trips?.items || [],
    pagination: data?.trips || null,
    isLoading,
    isError: error,
    mutate,
  };
}

/**
 * Hook to fetch user's favorite trips
 */
export function useFavoriteTrips(params = { pageNumber: 1, pageSize: 20 }) {
  const { data, error, isLoading, mutate } = useSWR(
    ["favorite-trips", params],
    () => userApi.getMyFavoriteTrips(params).then((res) => res.data),
  );

  return {
    trips: data?.items || [],
    pagination: data || null,
    isLoading,
    isError: error,
    mutate,
  };
}

export function useCompletedTrips(userId, pageNumber = 1, pageSize = 10) {
  return useSWR(userId ? ["completed-trips", userId, pageNumber] : null, () =>
    tripApi.getCompleted(userId, pageNumber, pageSize).then((res) => res.data),
  );
}
