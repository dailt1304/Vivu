import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { SWRConfig } from "swr";
import React from "react";
import {
  useAllCities,
  useCityById,
  useSearchCities,
  useCreateCity,
  useUpdateCity,
  useDeleteCity,
} from "../useCities";
import cityApi from "../../../api/cityApi";

// Mock cityApi
vi.mock("../../../api/cityApi", () => ({
  default: {
    getAll: vi.fn(),
    getById: vi.fn(),
    search: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

const wrapper = ({ children }) => (
  <SWRConfig value={{ provider: () => new Map(), dedupingInterval: 0 }}>
    {children}
  </SWRConfig>
);

describe("useCities hooks", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("useAllCities", () => {
    it("should fetch all cities", async () => {
      const mockData = { items: [{ id: "1", name: "Hanoi" }], totalCount: 1 };
      cityApi.getAll.mockResolvedValueOnce({ data: mockData });

      const { result } = renderHook(
        () => useAllCities({ pageNumber: 1, pageSize: 10 }),
        { wrapper },
      );

      await waitFor(() => expect(result.current.data).toEqual(mockData));
      expect(cityApi.getAll).toHaveBeenCalledWith({
        pageNumber: 1,
        pageSize: 10,
      });
    });
  });

  describe("useCityById", () => {
    it("should fetch city by id", async () => {
      const mockData = { id: "1", name: "Hanoi" };
      cityApi.getById.mockResolvedValueOnce({ data: mockData });

      const { result } = renderHook(() => useCityById("1"), { wrapper });

      await waitFor(() => expect(result.current.data).toEqual(mockData));
      expect(cityApi.getById).toHaveBeenCalledWith("1");
    });
  });

  describe("useSearchCities", () => {
    it("should search cities", async () => {
      const mockData = { items: [{ id: "1", name: "Hanoi" }], totalCount: 1 };
      cityApi.search.mockResolvedValueOnce({ data: mockData });

      const { result } = renderHook(() => useSearchCities(), { wrapper });

      await result.current.trigger({ searchText: "han" });
      expect(cityApi.search).toHaveBeenCalledWith({ searchText: "han" });
    });
  });

  describe("useCreateCity", () => {
    it("should create city", async () => {
      const mockData = { id: "1" };
      cityApi.create.mockResolvedValueOnce({ data: mockData });

      const { result } = renderHook(() => useCreateCity(), { wrapper });

      const data = { name: "Hanoi", countryId: "vn" };
      await result.current.trigger(data);
      expect(cityApi.create).toHaveBeenCalledWith(data);
    });
  });

  describe("useUpdateCity", () => {
    it("should update city", async () => {
      const mockData = { id: "1" };
      cityApi.update.mockResolvedValueOnce({ data: mockData });

      const { result } = renderHook(() => useUpdateCity(), { wrapper });

      const updatePayload = { id: "1", data: { name: "Hanoi" } };
      await result.current.trigger(updatePayload);
      expect(cityApi.update).toHaveBeenCalledWith("1", { name: "Hanoi" });
    });
  });

  describe("useDeleteCity", () => {
    it("should delete city", async () => {
      cityApi.delete.mockResolvedValueOnce({ data: true });

      const { result } = renderHook(() => useDeleteCity(), { wrapper });

      await result.current.trigger("1");
      expect(cityApi.delete).toHaveBeenCalledWith("1");
    });
  });
});
