import { describe, it, expect, vi, beforeEach } from "vitest";
import userApi from "../userApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe("userApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getById", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { id: "u1" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await userApi.getById("u1");

      expect(axiosClient.get).toHaveBeenCalledWith("/users/u1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getAll", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await userApi.getAll(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/users", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("search", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { searchTerm: "test", pageNumber: 1, pageSize: 10 };
      const result = await userApi.search(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/users/search", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("create", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { id: "u2" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com" };
      const result = await userApi.create(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/users", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getMyFavoriteTrips", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await userApi.getMyFavoriteTrips(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/users/me/favorites", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getUsageStats", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { used: 5, limit: 10, remaining: 5 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await userApi.getUsageStats();

      expect(axiosClient.get).toHaveBeenCalledWith("/users/usage");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("updateMyProfile", () => {
    it("should call axiosClient.put with correct URL and data", async () => {
      const mockResponse = { data: { id: "u1", fullName: "Updated Name" } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const data = { fullName: "Updated Name", bio: "New bio" };
      const result = await userApi.updateMyProfile(data);

      expect(axiosClient.put).toHaveBeenCalledWith("/users/me/profile", data);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("ban", () => {
    it("should call axiosClient.post with correct URL and reason", async () => {
      const mockResponse = { data: { id: "u1", status: "banned" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await userApi.ban("u1", "Violation");

      expect(axiosClient.post).toHaveBeenCalledWith("/users/u1/ban", {
        reason: "Violation",
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("unban", () => {
    it("should call axiosClient.post with correct URL", async () => {
      const mockResponse = { data: { id: "u1", status: "active" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await userApi.unban("u1");

      expect(axiosClient.post).toHaveBeenCalledWith("/users/u1/unban");
      expect(result).toEqual(mockResponse);
    });
  });
});
