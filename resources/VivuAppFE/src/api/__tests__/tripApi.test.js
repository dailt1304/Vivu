import { describe, it, expect, vi, beforeEach } from "vitest";
import tripApi from "../tripApi";
import axiosClient from "../axiosClient";

// Mock the axios client
vi.mock("../axiosClient", () => {
  return {
    default: {
      post: vi.fn(),
      delete: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      get: vi.fn(),
    },
  };
});

describe("tripApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getPublic", () => {
    it("should call axiosClient.get with correct URL and search params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 12, sortBy: "newest" };
      const result = await tripApi.getPublic(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/trips/public", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("searchPublic", () => {
    it("should call axiosClient.get with correct URL and search query", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { q: "Đà Nẵng", pageSize: 9 };
      const result = await tripApi.searchPublic(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/trips/public/search", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("favorite", () => {
    it("should call axiosClient.post with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const tripId = "trip-123";
      const result = await tripApi.favorite(tripId);

      expect(axiosClient.post).toHaveBeenCalledWith(
        `/trips/${tripId}/favorite`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("unfavorite", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const tripId = "trip-123";
      const result = await tripApi.unfavorite(tripId);

      expect(axiosClient.delete).toHaveBeenCalledWith(
        `/trips/${tripId}/favorite`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("updateVisibility", () => {
    it("should call axiosClient.patch with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.patch.mockResolvedValueOnce(mockResponse);

      const tripId = "trip-123";
      const payload = { visibility: 1 };
      const result = await tripApi.updateVisibility(tripId, payload);

      expect(axiosClient.patch).toHaveBeenCalledWith(
        `/trips/${tripId}/visibility`,
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getChatHistory", () => {
    it("should call axiosClient.get with correct URL and search params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const tripId = "trip-123";
      const params = { pageIndex: 1, pageSize: 50 };
      const result = await tripApi.getChatHistory(tripId, params);

      expect(axiosClient.get).toHaveBeenCalledWith(
        `/trips/${tripId}/messages`,
        {
          params,
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("copy", () => {
    it("should call POST /trips/:id/copy", async () => {
      const mockResponse = { data: { id: "new-trip-id" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const tripId = "trip-123";
      const result = await tripApi.copy(tripId);

      expect(axiosClient.post).toHaveBeenCalledWith(
        `/trips/${tripId}/copy`,
        {},
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("Core CRUD Operations", () => {
    it("should call POST /trips for create", async () => {
      const mockResponse = { data: { id: "1" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { name: "New Trip" };
      const result = await tripApi.create(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/trips", payload);
      expect(result).toEqual(mockResponse);
    });

    it("should call GET /trips/:id for getById", async () => {
      const mockResponse = { data: { id: "1", name: "Trip 1" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await tripApi.getById("1");

      expect(axiosClient.get).toHaveBeenCalledWith("/trips/1");
      expect(result).toEqual(mockResponse);
    });

    it("should call DELETE /trips/:id for delete", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const result = await tripApi.delete("1");

      expect(axiosClient.delete).toHaveBeenCalledWith("/trips/1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("updateStatuses", () => {
    it("should call POST /trips/update-statuses", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await tripApi.updateStatuses();

      expect(axiosClient.post).toHaveBeenCalledWith("/trips/update-statuses");
      expect(result).toEqual(mockResponse);
    });
  });
});
