import useSWRImmutable from "swr/immutable";

async function fetchCityByIP() {
  const res = await fetch("https://get.geojs.io/v1/ip/geo.json");
  if (!res.ok) throw new Error("IP lookup failed");
  return res.json();
}

/**
 * Custom hook to fetch user's city via IP Address.
 * Uses useSWRImmutable to prevent re-fetching across the entire session.
 */
export function useUserCity() {
  return useSWRImmutable("user-city-ip", fetchCityByIP, {
    revalidateOnFocus: false,
    shouldRetryOnError: false, // Don't retry if IP is blocked / failed
  });
}
