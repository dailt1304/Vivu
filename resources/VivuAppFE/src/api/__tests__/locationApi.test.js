import { describe, it, expect, vi, beforeEach } from "vitest";
import locationApi from "../locationApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("locationApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getPopular", () => {
    it("should call get with /locations/popular", async () => {
      const mockParams = { pageNumber: 1, pageSize: 6 };
      const expectedResponse = [{ id: "1", title: "Test" }];

      axiosClient.get.mockResolvedValueOnce({ data: expectedResponse });

      const result = await locationApi.getPopular(mockParams);

      expect(axiosClient.get).toHaveBeenCalledWith("/locations/popular", {
        params: mockParams,
      });
      expect(result.data).toEqual(expectedResponse);
    });
  });

  describe("getNearby", () => {
    it("should call get with /locations/nearby", async () => {
      const mockParams = {
        latitude: 16.047,
        longitude: 108.206,
        radiusInMeters: 5000,
      };
      const expectedResponse = [{ id: "1", title: "Nearby Place" }];

      axiosClient.get.mockResolvedValueOnce({ data: expectedResponse });

      const result = await locationApi.getNearby(mockParams);

      expect(axiosClient.get).toHaveBeenCalledWith("/locations/nearby", {
        params: mockParams,
      });
      expect(result.data).toEqual(expectedResponse);
    });
  });

  describe("getAll", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await locationApi.getAll(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/locations", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getByFilter", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { CityId: "c1" };
      const result = await locationApi.getByFilter(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/locations/get-by-filter", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getById", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { id: "loc1" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await locationApi.getById("loc1");

      expect(axiosClient.get).toHaveBeenCalledWith("/locations/loc1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("submit", () => {
    it("should call axiosClient.post with correct URL, payload, and headers", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const formData = new FormData();
      formData.append("name", "Test");
      const result = await locationApi.submit(formData);

      expect(axiosClient.post).toHaveBeenCalledWith("/locations", formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("report", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { locationId: "loc1", reason: "Spam" };
      const result = await locationApi.report(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/locations/report",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getMySubmissions", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await locationApi.getMySubmissions(params);

      expect(axiosClient.get).toHaveBeenCalledWith(
        "/locations/my-submissions",
        { params },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getPending", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: [] };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await locationApi.getPending(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/locations/pending", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("searchTerm", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: [] };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { searchTerm: "cafe" };
      const result = await locationApi.searchTerm(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/locations/search-term",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("update", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const payload = { name: "Updated" };
      const result = await locationApi.update("loc1", payload);

      expect(axiosClient.put).toHaveBeenCalledWith("/locations/loc1", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("suggestUpdate", () => {
    it("should call axiosClient.put with correct URL, payload, and headers", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const formData = new FormData();
      formData.append("name", "New Name");
      const result = await locationApi.suggestUpdate("loc1", formData);

      expect(axiosClient.put).toHaveBeenCalledWith(
        "/locations/loc1/suggest-update",
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("delete", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const result = await locationApi.delete("loc1");

      expect(axiosClient.delete).toHaveBeenCalledWith("/locations/loc1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("rejectSuggestion", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { adminNote: "Duplicate" };
      const result = await locationApi.rejectSuggestion("loc1", payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/locations/loc1/reject-suggestion",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("approve", () => {
    it("should call axiosClient.put with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const payload = { adminNote: "Approved" };
      const result = await locationApi.approve("loc1", payload);

      expect(axiosClient.put).toHaveBeenCalledWith(
        "/locations/loc1/approve",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
