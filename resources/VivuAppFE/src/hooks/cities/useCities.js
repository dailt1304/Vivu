import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import cityApi from "../../api/cityApi";

/**
 * Hook to get all cities with pagination.
 * @param {Object} params - { pageNumber, pageSize, countryId, sortColumn, sortDescending }
 */
export function useAllCities(params) {
  const key = params ? ["all-cities", params] : null;

  return useSWR(key, () => cityApi.getAll(params).then((res) => res.data));
}

/**
 * Hook to get city details by ID.
 * @param {string} id - City ID
 */
export function useCityById(id) {
  return useSWR(id ? `city-${id}` : null, () =>
    cityApi.getById(id).then((res) => res.data),
  );
}

/**
 * Hook to search cities.
 * Uses useSWRMutation for manual triggering (e.g., in a search bar).
 */
export function useSearchCities() {
  return useSWRMutation("search-cities", (_, { arg: params }) =>
    cityApi.search(params).then((res) => res.data),
  );
}

/**
 * Mutation hook for creating a city.
 */
export function useCreateCity() {
  return useSWRMutation("create-city", (_, { arg: data }) =>
    cityApi.create(data).then((res) => res.data),
  );
}

/**
 * Mutation hook for updating a city.
 */
export function useUpdateCity() {
  return useSWRMutation("update-city", (_, { arg: { id, data } }) =>
    cityApi.update(id, data).then((res) => res.data),
  );
}

/**
 * Mutation hook for deleting a city.
 */
export function useDeleteCity() {
  return useSWRMutation("delete-city", (_, { arg: id }) =>
    cityApi.delete(id).then((res) => res.data),
  );
}
