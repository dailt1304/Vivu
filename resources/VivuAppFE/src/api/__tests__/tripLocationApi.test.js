import { describe, it, expect, vi, beforeEach } from "vitest";
import tripLocationApi from "../tripLocationApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("tripLocationApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("add", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { tripDayId: "d1", locationId: "loc1", orderIndex: 0 };
      const result = await tripLocationApi.add(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/triplocation", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("update", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const id = "tl1";
      const payload = { orderIndex: 1 };
      const result = await tripLocationApi.update(id, payload);

      expect(axiosClient.put).toHaveBeenCalledWith(
        `/triplocation/${id}`,
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("remove", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const id = "tl1";
      const result = await tripLocationApi.remove(id);

      expect(axiosClient.delete).toHaveBeenCalledWith(`/triplocation/${id}`);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("reorder", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const payload = {
        tripDayId: "d1",
        OrderedTripLocationIds: ["tl2", "tl1"],
      };
      const result = await tripLocationApi.reorder(payload);

      expect(axiosClient.put).toHaveBeenCalledWith(
        "/triplocation/reorder",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
