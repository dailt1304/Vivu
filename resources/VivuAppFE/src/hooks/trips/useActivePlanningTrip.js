import useSWRImmutable from "swr/immutable";
import tripApi from "../../api/tripApi";

/**
 * Custom hook to fetch the most recent active "planning" trip for the given user.
 * Uses useSWRImmutable so it fetches once per session/tab lifecycle,
 * prioritizing the most recently created planning trip.
 * 
 * @param {string|null} userId - The current user's ID
 * @returns {{ planningTrip: Object | null, isLoading: boolean, error: Error | null }}
 */
export function useActivePlanningTrip(userId) {
  // Only fetch if we have a valid userId
  const { data, error, isLoading } = useSWRImmutable(
    userId ? ["active-planning-trip", userId] : null,
    async ([_, uid]) => {
      // Fetch 1 planning trip (API already sorts by CreatedDate desc)
      const response = await tripApi.getAllByUser(uid, {
        status: "planning",
        pageSize: 1,
        pageNumber: 1
      });

      if (response.success && response.data?.trips?.items?.length > 0) {
        return response.data.trips.items[0];
      }
      return null;
    },
    {
      revalidateOnFocus: false, // Don't revalidate on tab switch to avoid layout shifts
      shouldRetryOnError: false
    }
  );

  return {
    planningTrip: data,
    isLoading,
    error
  };
}
