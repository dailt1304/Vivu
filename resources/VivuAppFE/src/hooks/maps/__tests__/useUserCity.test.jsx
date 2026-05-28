import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useUserCity } from "../useUserCity";
import { SWRConfig } from "swr";

describe("useUserCity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const wrapper = ({ children }) => (
    <SWRConfig value={{ provider: () => new Map() }}>{children}</SWRConfig>
  );

  it("should fetch and return city data from IP successfully", async () => {
    const mockResponse = { city: "Ho Chi Minh City", country: "VN" };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(mockResponse),
    });

    const { result } = renderHook(() => useUserCity(), { wrapper });

    await waitFor(() => {
      expect(result.current.data).toEqual(mockResponse);
    });

    expect(global.fetch).toHaveBeenCalledWith(
      "https://get.geojs.io/v1/ip/geo.json",
    );
  });

  it("should return error if API request fails", async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
    });

    const { result } = renderHook(() => useUserCity(), { wrapper });

    await waitFor(() => {
      expect(result.current.error).toBeDefined();
      expect(result.current.error?.message).toBe("IP lookup failed");
    });
  });
});
