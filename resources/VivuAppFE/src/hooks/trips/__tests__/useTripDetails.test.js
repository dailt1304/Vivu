import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act, waitFor } from "@testing-library/react";
import { addDays } from "date-fns";
import { http, HttpResponse } from "msw";
import { server } from "../../../test/mocks/server";
import { mockTrip } from "../../../test/mocks/handlers";
import useTripDetails from "../useTripDetails";
import toast from "../../../utils/toast";

const API_URL = "https://localhost:7294/api";

vi.mock("../../../utils/toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
  },
}));

describe("useTripDetails", () => {
  const extendedMockTrip = {
    ...mockTrip,
    tripDays: [
      { id: "day1", dayIndex: 1, locations: [{ id: "loc1" }] },
      { id: "day2", dayIndex: 2, locations: [] },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    server.resetHandlers();
    // Override the default mockTrip with our extended one for these tests
    server.use(
      http.get(`${API_URL}/trips/:id`, () =>
        HttpResponse.json({ success: true, data: extendedMockTrip }),
      ),
    );
  });

  afterEach(() => {
    server.restoreHandlers();
  });

  it("1. should fetch trip successfully when tripId is provided", async () => {
    const { result } = renderHook(() => useTripDetails("trip1"));

    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.trip).toEqual(extendedMockTrip);
    expect(result.current.error).toBeNull();
  });

  it("2. should set error if trip not found (success: false)", async () => {
    server.use(
      http.get(`${API_URL}/trips/:id`, () =>
        HttpResponse.json({ success: false, message: "Not found" }),
      ),
    );

    const { result } = renderHook(() => useTripDetails("wrong-id"));

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.trip).toBeNull();
    expect(result.current.error).toBe("Không tìm thấy chuyến đi");
  });

  it("3. should handle network error on fetch", async () => {
    server.use(http.get(`${API_URL}/trips/:id`, () => HttpResponse.error()));

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.loading).toBe(false));

    expect(result.current.error).toBe("Không thể tải thông tin chuyến đi");
  });

  it("4. should not attempt to fetch when tripId is falsy", async () => {
    const { result } = renderHook(() => useTripDetails(null));

    expect(result.current.trip).toBeNull();
  });

  it("5. should update a single field successfully", async () => {
    let capturedPayload = null;
    server.use(
      http.put(`${API_URL}/trips/:id`, async ({ request }) => {
        capturedPayload = await request.json();
        return HttpResponse.json({ success: true });
      }),
    );

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    let updateSuccess;
    await act(async () => {
      updateSuccess = await result.current.updateField(
        "title",
        "New Trip Title",
      );
    });

    expect(updateSuccess).toBe(true);
    expect(capturedPayload.title).toBe("New Trip Title");
    expect(result.current.trip.title).toBe("New Trip Title");
    expect(toast.success).toHaveBeenCalledWith("Đã cập nhật!");
  });

  it("6. should return false from updateField if value is empty", async () => {
    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    let updateSuccess;
    await act(async () => {
      updateSuccess = await result.current.updateField("title", "");
    });

    expect(updateSuccess).toBe(false);
  });

  it("7. should add a new day when updating dates to extend the trip", async () => {
    let addedDaysCount = 0;
    server.use(
      http.post(`${API_URL}/tripday`, () => {
        addedDaysCount++;
        return HttpResponse.json({ success: true, data: { dayIndex: 3 } });
      }),
    );

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    const startDate = new Date("2026-03-01T00:00:00Z");
    const endDate = new Date("2026-03-03T00:00:00Z");

    let success;
    await act(async () => {
      success = await result.current.updateDates(startDate, endDate);
    });

    expect(success).toBe(true);
    expect(addedDaysCount).toBe(1);
    expect(toast.success).toHaveBeenCalledWith("Đã thêm 1 ngày mới!");
  });

  it("8. should remove empty days when updating dates to shorten the trip", async () => {
    const mockTrip3Days = {
      ...extendedMockTrip,
      tripDays: [
        { id: "day1", locations: [{ id: "l1" }] },
        { id: "day2", locations: [] },
        { id: "day3", locations: [] },
      ],
    };

    let removedDaysCount = 0;
    server.use(
      http.get(`${API_URL}/trips/:id`, () =>
        HttpResponse.json({ success: true, data: mockTrip3Days }),
      ),
      http.delete(`${API_URL}/tripday/:dayId`, () => {
        removedDaysCount++;
        return HttpResponse.json({ success: true });
      }),
    );

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    const startDate = new Date("2026-03-01T00:00:00Z");
    const endDate = new Date("2026-03-02T00:00:00Z");

    let success;
    await act(async () => {
      success = await result.current.updateDates(startDate, endDate);
    });

    expect(success).toBe(true);
    expect(removedDaysCount).toBe(1);
    expect(toast.success).toHaveBeenCalledWith("Đã xóa 1 ngày!");
  });

  it("9. should warn and prevent date shortening if excess days have locations", async () => {
    const mockTripWithLocsOnLastDay = {
      ...extendedMockTrip,
      tripDays: [
        { id: "day1", locations: [] },
        { id: "day2", locations: [{ id: "must-not-delete" }] },
      ],
    };

    server.use(
      http.get(`${API_URL}/trips/:id`, () =>
        HttpResponse.json({ success: true, data: mockTripWithLocsOnLastDay }),
      ),
    );

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    const startDate = new Date("2026-03-01T00:00:00Z");
    const endDate = new Date("2026-03-01T00:00:00Z");

    let success;
    await act(async () => {
      success = await result.current.updateDates(startDate, endDate);
    });

    expect(success).toBe(false);
    expect(toast.warning).toHaveBeenCalledWith(
      expect.stringContaining("Không thể giảm ngày vì 1 ngày cuối có địa điểm"),
    );
  });

  it("10. saveTitle should trim the editableTitle and save it", async () => {
    let capturedPayload = null;
    server.use(
      http.put(`${API_URL}/trips/:id`, async ({ request }) => {
        capturedPayload = await request.json();
        return HttpResponse.json({ success: true });
      }),
    );

    const { result } = renderHook(() => useTripDetails("trip1"));

    await waitFor(() => expect(result.current.trip).not.toBeNull());

    act(() => {
      result.current.setEditableTitle("   My Awesome Trip   ");
    });

    await act(async () => {
      await result.current.saveTitle();
    });

    expect(capturedPayload.title).toBe("My Awesome Trip");
  });
});
