import { describe, it, expect, vi, beforeEach } from "vitest";
import cityApi from "../cityApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("cityApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getAll", () => {
    it("should call axiosClient.get with /cities and params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 10 };
      const result = await cityApi.getAll(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/cities", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getById", () => {
    it("should call axiosClient.get with /cities/:id", async () => {
      const mockResponse = { data: { id: "1", name: "Hanoi" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await cityApi.getById("1");

      expect(axiosClient.get).toHaveBeenCalledWith("/cities/1");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("search", () => {
    it("should call axiosClient.get with /cities/search and params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { searchText: "han", pageNumber: 1, pageSize: 10 };
      const result = await cityApi.search(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/cities/search", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("create", () => {
    it("should call axiosClient.post with /cities and data", async () => {
      const mockResponse = { data: { id: "1" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const data = { name: "Hanoi", countryId: "vn" };
      const result = await cityApi.create(data);

      expect(axiosClient.post).toHaveBeenCalledWith("/cities", data);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("update", () => {
    it("should call axiosClient.put with /cities/:id and data", async () => {
      const mockResponse = { data: { id: "1" } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const data = { name: "Hanoi Updated" };
      const result = await cityApi.update("1", data);

      expect(axiosClient.put).toHaveBeenCalledWith("/cities/1", data);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("delete", () => {
    it("should call axiosClient.delete with /cities/:id", async () => {
      const mockResponse = { data: true };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const result = await cityApi.delete("1");

      expect(axiosClient.delete).toHaveBeenCalledWith("/cities/1");
      expect(result).toEqual(mockResponse);
    });
  });
});
