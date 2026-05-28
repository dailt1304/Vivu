import useSWR from "swr";
import tripApi from "../../api/tripApi";
import { useDebounce } from "../utils";

/**
 * Hook to fetch paginated public trips
 */
export function usePublicTrips(params) {
  // If params is null, SWR will not fetch (useful for lazy loading tabs)
  const key = params ? ["public-trips", JSON.stringify(params)] : null;

  return useSWR(key, () => tripApi.getPublic(params).then((r) => r.data));
}

/**
 * Hook to fetch top trending trips
 */
export function useTrendingTrips(pageSize = 6) {
  return useSWR(
    ["trending-trips", pageSize],
    () =>
      tripApi.getPublic({ sortBy: "popular", pageSize }).then((r) => r.data),
    {
      revalidateOnFocus: false,
      revalidateIfStale: false,
    },
  );
}

/**
 * Hook tìm kiếm trips công khai với auto-debounce
 * - Chỉ gọi API khi searchTerm >= 2 ký tự (tránh spam request)
 * - SWR tự cache kết quả đã search (gõ lại = instant)
 * - Debounce 400ms để user gõ xong mới fetch
 */
export function useSearchPublicTrips(searchTerm, pageSize = 6, pageNumber = 1) {
  const debouncedTerm = useDebounce(searchTerm, 400);

  // SWR key = null khi chưa đủ ký tự -> không fetch
  const key =
    debouncedTerm?.length >= 2
      ? ["search-public-trips", debouncedTerm, pageSize, pageNumber]
      : null;

  return useSWR(
    key,
    () =>
      tripApi
        .searchPublic({ q: debouncedTerm, pageSize, pageNumber })
        .then((r) => r.data),
    { keepPreviousData: true },
  );
}
