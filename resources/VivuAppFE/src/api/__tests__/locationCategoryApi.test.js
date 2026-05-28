import { describe, it, expect, vi, beforeEach } from "vitest";
import locationCategoryApi from "../locationCategoryApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
  },
}));

describe("locationCategoryApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getAll", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await locationCategoryApi.getAll();

      expect(axiosClient.get).toHaveBeenCalledWith("/location-categories");
      expect(result).toEqual(mockResponse);
    });
  });
});
