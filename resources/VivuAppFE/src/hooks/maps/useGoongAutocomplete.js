import useSWR from "swr";
import goongApi from "../../api/goongApi";
import { useDebounce } from "../utils";

// SWR hook for Goong Autocomplete
export function useGoongAutocomplete(input, sessionToken) {
  const debouncedInput = useDebounce(input, 300);

  // Vercel Rule: client-swr-dedup
  return useSWR(
    debouncedInput?.length >= 2
      ? ["goong-autocomplete", debouncedInput, sessionToken]
      : null,
    () => goongApi.autocomplete(debouncedInput, sessionToken),
    {
      revalidateOnFocus: false, // Don't refetch when window gets focus
      dedupingInterval: 2000, // Dedupe requests within 2 seconds
    },
  );
}
