import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useCompletedTrips } from "../useTrips";
import tripApi from "../../../api/tripApi";

vi.mock("../../../api/tripApi", () => ({
  default: {
    getCompleted: vi.fn(),
  },
}));

describe("useTrips", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should fetch completed trips when userId is provided", async () => {
    const mockData = [{ id: "trip1", title: "Da Nang" }];
    tripApi.getCompleted.mockResolvedValue({ data: mockData });

    const { result } = renderHook(() => useCompletedTrips("user1", 1, 10));

    await waitFor(() => {
      expect(result.current.data).toEqual(mockData);
    });

    expect(tripApi.getCompleted).toHaveBeenCalledWith("user1", 1, 10);
  });

  it("should not fetch when userId is falsy (lazy fetching)", async () => {
    const { result } = renderHook(() => useCompletedTrips(null));

    expect(result.current.data).toBeUndefined();
    expect(tripApi.getCompleted).not.toHaveBeenCalled();
  });
});
