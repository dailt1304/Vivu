import useSWR from "swr";
import tripApi from "../../api/tripApi";

/**
 * Hook to fetch all images uploaded via chat in a trip.
 * Follows Vercel Rule: client-swr-dedup.
 * @param {string} tripId - The Trip ID.
 * @returns {object} { images, isLoading, error, mutate }
 */
export function useTripImages(tripId) {
  const key = tripId ? ["trip-images", tripId] : null;

  const { data, error, isLoading, mutate } = useSWR(key, ([, id]) =>
    tripApi.getTripImages(id).then((res) => res.data),
  );

  return {
    images: data,
    isLoading,
    error,
    mutate,
  };
}
