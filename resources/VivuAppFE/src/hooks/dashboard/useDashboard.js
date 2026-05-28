import useSWR from "swr";
import statisticsApi from "@/api/statisticsApi";

/**
 * Hook to fetch all dashboard statistics from the unified endpoint.
 * Returns: { users, revenue, content, locations } matching AdminDashboardDto
 */
export const useDashboardData = () => {
  const fetcher = async () => {
    const response = await statisticsApi.getDashboardStats();
    return response.data;
  };

  const { data, error, isLoading, mutate } = useSWR(
    "admin/dashboard",
    fetcher,
    {
      refreshInterval: 5 * 60 * 1000, // 5 minutes
      revalidateOnFocus: true,
    },
  );

  return {
    dashboardData: data,
    isLoading,
    isError: error,
    refresh: mutate,
  };
};
