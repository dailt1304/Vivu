import { describe, it, expect, vi, beforeEach } from "vitest";
import tripDayApi from "../tripDayApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("tripDayApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("add", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { tripId: "t1", dayDate: "2026-03-01T00:00:00Z" };
      const result = await tripDayApi.add(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/tripday", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("update", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const tripDayId = "d1";
      const payload = { title: "Day 1" };
      const result = await tripDayApi.update(tripDayId, payload);

      expect(axiosClient.put).toHaveBeenCalledWith(
        `/tripday/${tripDayId}`,
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("remove", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const tripDayId = "d1";
      const result = await tripDayApi.remove(tripDayId);

      expect(axiosClient.delete).toHaveBeenCalledWith(`/tripday/${tripDayId}`);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("reorder", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const tripId = "t1";
      const orderedTripDayIds = ["d2", "d1"];
      const result = await tripDayApi.reorder(tripId, orderedTripDayIds);

      expect(axiosClient.put).toHaveBeenCalledWith(
        `/tripday/reorder/${tripId}`,
        { orderedTripDayIds },
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
