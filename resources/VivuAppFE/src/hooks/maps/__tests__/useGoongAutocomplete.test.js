import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useGoongAutocomplete } from "../useGoongAutocomplete";
import useSWR from "swr";
import goongApi from "../../../api/goongApi";

// Mock the API
vi.mock("../../../api/goongApi", () => ({
  default: {
    autocomplete: vi.fn(),
  },
}));

// Mock useSWR directly to bypass tricky cache/dedupe logic
vi.mock("swr", () => ({
  default: vi.fn(),
}));

// Mock useDebounce so it returns immediately
vi.mock("../../utils/useDebounce", () => ({
  default: (val) => val,
}));

describe("useGoongAutocomplete", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should return null for SWR key if input is less than 2 characters", () => {
    // SWR mock returns empty
    useSWR.mockReturnValue({ data: undefined });

    renderHook(() => useGoongAutocomplete("A", "session-123"));

    // useSWR signature: useSWR(key, fetcher, options)
    // We expect the key to be null since debouncedInput length < 2
    expect(useSWR).toHaveBeenCalledWith(
      null,
      expect.any(Function),
      expect.any(Object),
    );
  });

  it("should provide valid SWR key and call API fetcher if input >= 2 chars", async () => {
    const mockResults = [{ description: "Ha Noi", place_id: "hn1" }];
    goongApi.autocomplete.mockResolvedValue({ predictions: mockResults });
    useSWR.mockReturnValue({ data: { predictions: mockResults } });

    const { result } = renderHook(() =>
      useGoongAutocomplete("Ha", "session-123"),
    );

    // Check SWR is called with correct key
    expect(useSWR).toHaveBeenCalledWith(
      ["goong-autocomplete", "Ha", "session-123"],
      expect.any(Function),
      expect.any(Object),
    );

    // Get the fetcher function passed to useSWR and call it manually
    // to verify it correctly maps to goongApi.autocomplete
    const fetcher = useSWR.mock.calls[0][1];

    const fetcherResult = await fetcher();

    expect(goongApi.autocomplete).toHaveBeenCalledWith("Ha", "session-123");
    expect(fetcherResult).toEqual({ predictions: mockResults });

    // Check hook output
    expect(result.current.data).toEqual({ predictions: mockResults });
  });
});
