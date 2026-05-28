import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useDashboardData } from "../useDashboard";
import statisticsApi from "@/api/statisticsApi";
import React from "react";

// Mock SWR to avoid real network calls and allow controlled data
vi.mock("swr", () => ({
  default: vi.fn((key, fetcher) => {
    const [data, setData] = React.useState(null);
    const [isLoading, setIsLoading] = React.useState(true);
    const [error, setError] = React.useState(null);

    React.useEffect(() => {
      fetcher()
        .then((d) => {
          setData(d);
          setIsLoading(false);
        })
        .catch((e) => {
          setError(e);
          setIsLoading(false);
        });
    }, []);

    return { data, error, isLoading, mutate: vi.fn() };
  }),
}));

vi.mock("@/api/statisticsApi", () => ({
  default: {
    getLocationStats: vi.fn(),
    getUserStats: vi.fn(),
    getRevenueStats: vi.fn(),
    getSystemActivities: vi.fn(),
  },
}));

describe("useDashboardData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch all stats in parallel and return aggregated data", async () => {
    const mockLocations = { data: { pendingSubmissionsCount: 5 } };
    const mockUsers = { data: { totalUsers: 100 } };
    const mockRevenue = { data: { monthlyRevenue: 5000 } };
    const mockActivities = { data: { recent: [] } };

    statisticsApi.getLocationStats.mockResolvedValue(mockLocations);
    statisticsApi.getUserStats.mockResolvedValue(mockUsers);
    statisticsApi.getRevenueStats.mockResolvedValue(mockRevenue);
    statisticsApi.getSystemActivities.mockResolvedValue(mockActivities);

    const { result } = renderHook(() => useDashboardData());

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.dashboardData).toEqual({
      locations: mockLocations.data,
      users: mockUsers.data,
      revenue: mockRevenue.data,
      activities: mockActivities.data,
    });

    expect(statisticsApi.getLocationStats).toHaveBeenCalled();
    expect(statisticsApi.getUserStats).toHaveBeenCalled();
    expect(statisticsApi.getRevenueStats).toHaveBeenCalled();
    expect(statisticsApi.getSystemActivities).toHaveBeenCalled();
  });
});
