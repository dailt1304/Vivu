import { describe, it, expect, vi, beforeEach } from "vitest";
import statisticsApi from "../statisticsApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
  },
}));

describe("statisticsApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getLocationStats", () => {
    it("should call get with /statistics/locations", async () => {
      const expectedResponse = { data: { totalUsers: 10 } };
      axiosClient.get.mockResolvedValueOnce(expectedResponse);

      const result = await statisticsApi.getLocationStats();

      expect(axiosClient.get).toHaveBeenCalledWith("/statistics/locations");
      expect(result).toEqual(expectedResponse);
    });
  });
});
