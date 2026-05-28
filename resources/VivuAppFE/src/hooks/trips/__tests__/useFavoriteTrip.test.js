import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import useFavoriteTrip from "../useFavoriteTrip";
import tripApi from "../../../api/tripApi";

// Mock dependencies
vi.mock("../../../api/tripApi", () => ({
  default: {
    favorite: vi.fn(),
    unfavorite: vi.fn(),
  },
}));

vi.mock("../../../utils/toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

const mockMutate = vi.fn();
vi.mock("swr", async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    useSWRConfig: () => ({ mutate: mockMutate }),
  };
});

describe("useFavoriteTrip", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should initialize with provided initial state", () => {
    const { result } = renderHook(() => useFavoriteTrip("trip-123", true));

    expect(result.current.isFavorited).toBe(true);
    expect(result.current.isLoading).toBe(false);
  });

  it("should toggle favorite from false to true", async () => {
    tripApi.favorite.mockResolvedValueOnce({ data: { success: true } });

    const { result } = renderHook(() => useFavoriteTrip("trip-123", false));

    await act(async () => {
      await result.current.toggleFavorite();
    });

    expect(result.current.isFavorited).toBe(true);
    expect(tripApi.favorite).toHaveBeenCalledWith("trip-123");
  });

  it("should toggle favorite from true to false", async () => {
    tripApi.unfavorite.mockResolvedValueOnce({ data: { success: true } });

    const { result } = renderHook(() => useFavoriteTrip("trip-123", true));

    await act(async () => {
      await result.current.toggleFavorite();
    });

    expect(result.current.isFavorited).toBe(false);
    expect(tripApi.unfavorite).toHaveBeenCalledWith("trip-123");
  });

  it("should rollback state if API call fails", async () => {
    tripApi.favorite.mockRejectedValueOnce(new Error("Network error"));

    const { result } = renderHook(() => useFavoriteTrip("trip-123", false));

    await act(async () => {
      await result.current.toggleFavorite();
    });

    // Expect rollback to initial state (false)
    expect(result.current.isFavorited).toBe(false);
    expect(tripApi.favorite).toHaveBeenCalledWith("trip-123");
  });
});
