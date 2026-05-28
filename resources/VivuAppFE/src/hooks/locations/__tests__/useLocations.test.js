import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import {
  useLocationsByFilter,
  useLocationById,
  useLocationCategories,
  useMySubmissions,
  useSubmitLocation,
  useReportLocation,
} from "../useLocations";
import locationApi from "../../../api/locationApi";
import locationCategoryApi from "../../../api/locationCategoryApi";

// Mock API modules
vi.mock("../../../api/locationApi", () => ({
  default: {
    getByFilter: vi.fn(),
    getById: vi.fn(),
    getMySubmissions: vi.fn(),
    submit: vi.fn(),
    report: vi.fn(),
  },
}));

vi.mock("../../../api/locationCategoryApi", () => ({
  default: {
    getAll: vi.fn(),
  },
}));

describe("useLocations", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("useLocationsByFilter", () => {
    it("should fetch locations when params are provided", async () => {
      const mockData = [{ id: 1, name: "Loc 1" }];
      locationApi.getByFilter.mockResolvedValue({ data: mockData });

      const { result } = renderHook(() =>
        useLocationsByFilter({ city: "HCM" }),
      );

      await waitFor(() => {
        expect(result.current.data).toEqual(mockData);
      });
      expect(locationApi.getByFilter).toHaveBeenCalledWith({ city: "HCM" });
    });

    it("should not fetch when params are falsy (SWR conditional fetching)", async () => {
      const { result } = renderHook(() => useLocationsByFilter(null));

      // Data is undefined because fetch never happened
      expect(result.current.data).toBeUndefined();
      expect(locationApi.getByFilter).not.toHaveBeenCalled();
    });
  });

  describe("useLocationById", () => {
    it("should fetch single location when id is provided", async () => {
      const mockLocation = { id: "123", name: "Landmark 81" };
      locationApi.getById.mockResolvedValue({ data: mockLocation });

      const { result } = renderHook(() => useLocationById("123"));

      await waitFor(() => {
        expect(result.current.data).toEqual(mockLocation);
      });
      expect(locationApi.getById).toHaveBeenCalledWith("123");
    });
  });

  describe("useLocationCategories", () => {
    it("should fetch all location categories", async () => {
      const mockCategories = [{ id: 1, type: "Nature" }];
      locationCategoryApi.getAll.mockResolvedValue({ data: mockCategories });

      const { result } = renderHook(() => useLocationCategories());

      await waitFor(() => {
        expect(result.current.data).toEqual(mockCategories);
      });
      expect(locationCategoryApi.getAll).toHaveBeenCalled();
    });
  });

  describe("useMySubmissions", () => {
    it("should fetch user's submitted locations when params provided", async () => {
      const mockData = [{ id: 1, name: "My submission" }];
      locationApi.getMySubmissions.mockResolvedValue({ data: mockData });

      const { result } = renderHook(() => useMySubmissions({ page: 1 }));

      await waitFor(() => {
        expect(result.current.data).toEqual(mockData);
      });
      expect(locationApi.getMySubmissions).toHaveBeenCalledWith({ page: 1 });
    });
  });
});
