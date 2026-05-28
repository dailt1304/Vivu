import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { renderHook, waitFor, act } from "@testing-library/react";
import useUserTrips from "../useUserTrips";
import { AuthProvider } from "../../../contexts/AuthContext";
import { useAuth } from "../../../contexts/auth-context";
import { MemoryRouter } from "react-router-dom";
import { http, HttpResponse } from "msw";
import { server } from "../../../test/mocks/server";

// Mock the Auth hook strictly for these tests
vi.mock("../../../contexts/auth-context", async () => {
  const actual = await vi.importActual("../../../contexts/auth-context");
  return {
    ...actual,
    useAuth: vi.fn(),
  };
});

describe("useUserTrips", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    server.resetHandlers();
    useAuth.mockReturnValue({
      userId: "user-1",
      isAuthenticated: true,
    });
  });

  afterEach(() => {
    server.restoreHandlers();
  });

  const wrapper = ({ children }) => (
    <MemoryRouter>
      <AuthProvider>{children}</AuthProvider>
    </MemoryRouter>
  );

  it("should fetch and transform trips on mount if authenticated", async () => {
    // Setup MSW to return mock trip data
    server.use(
      http.get("https://localhost:7294/api/trips/user/user-1", () => {
        return HttpResponse.json({
          success: true,
          data: {
            trips: {
              items: [
                {
                  id: "trip-1",
                  title: "My Trip",
                  userId: "user-1",
                  role: "Owner",
                  startDate: "2026-03-01T00:00:00",
                  endDate: "2026-03-05T00:00:00",
                  isDelete: false,
                },
                {
                  id: "trip-2",
                  title: "Shared Trip",
                  userId: "user-999",
                  role: "Member",
                  startDate: "2026-04-01T00:00:00",
                  endDate: "2026-04-03T00:00:00",
                  isDelete: false,
                },
              ],
            },
          },
        });
      }),
    );

    const { result } = renderHook(() => useUserTrips(), { wrapper });

    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBeNull();
    // 2 total trips
    expect(result.current.trips).toHaveLength(2);

    // Separation of owned vs shared
    expect(result.current.ownedTrips).toHaveLength(1);
    expect(result.current.ownedTrips[0].id).toBe("trip-1");
    expect(result.current.ownedTrips[0].isOwner).toBe(true);

    expect(result.current.sharedTrips).toHaveLength(1);
    expect(result.current.sharedTrips[0].id).toBe("trip-2");
    expect(result.current.sharedTrips[0].isOwner).toBe(false);

    // Format tests
    expect(result.current.trips[0].days).toBe(5); // Mar 1 to 5 inclusive
  });

  it("should handle error during fetch", async () => {
    server.use(
      http.get("https://localhost:7294/api/trips/user/user-1", () => {
        return new HttpResponse(null, { status: 500 });
      }),
    );

    const { result } = renderHook(() => useUserTrips(), { wrapper });

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBe("Không thể tải danh sách chuyến đi");
    expect(result.current.trips).toHaveLength(0);
  });

  it("should not fetch if not authenticated", () => {
    useAuth.mockReturnValue({
      userId: null,
      isAuthenticated: false,
    });

    const { result } = renderHook(() => useUserTrips(), { wrapper });

    expect(result.current.loading).toBe(false);
    expect(result.current.trips).toHaveLength(0);
  });

  it("should handle trip deletion via optimistic update", async () => {
    server.use(
      http.get("https://localhost:7294/api/trips/user/user-1", () =>
        HttpResponse.json({
          success: true,
          data: {
            trips: { items: [{ id: "trip-to-delete", isDelete: false }] },
          },
        }),
      ),
      http.delete("https://localhost:7294/api/trips/trip-to-delete", () =>
        HttpResponse.json({ success: true }),
      ),
    );

    const { result } = renderHook(() => useUserTrips(), { wrapper });

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
      expect(result.current.trips).toHaveLength(1);
    });

    let success;
    await act(async () => {
      success = await result.current.deleteTrip("trip-to-delete");
    });

    expect(success).toBe(true);
    expect(result.current.trips).toHaveLength(0); // Optimistically removed
  });
});
