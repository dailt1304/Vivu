import useSWR from "swr";
import userApi from "../api/userApi";

/**
 * Hook to fetch user by ID
 */
export function useUser(id) {
  const key = id ? ["user", id] : null;
  return useSWR(key, () => userApi.getById(id).then((res) => res.data));
}
