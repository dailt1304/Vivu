import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import TripItineraryEditor from "../TripItineraryEditor";

// Mock useTripDetails hook
vi.mock("../../../../../hooks/trips/useTripDetails", () => ({
  default: vi.fn(),
}));

import useTripDetails from "../../../../../hooks/trips/useTripDetails";

const mockTrip = {
  id: "trip-1",
  title: "Trip to Saigon",
  tripDays: [
    {
      dayIndex: 1,
      title: "Ngày 1",
      dayDate: "2026-01-15T00:00:00Z",
      locations: [
        {
          locationId: "loc-1",
          orderIndex: 1,
          location: { name: "Bến Nhà Rồng" },
        },
        {
          locationId: "loc-2",
          orderIndex: 2,
          location: { name: "Chợ Bến Thành" },
        },
      ],
    },
    {
      dayIndex: 2,
      title: "Ngày 2",
      dayDate: "2026-01-16T00:00:00Z",
      locations: [
        {
          locationId: "loc-3",
          orderIndex: 1,
          location: { name: "Cầu Rồng" },
        },
      ],
    },
  ],
};

describe("TripItineraryEditor", () => {
  let onStoryDaysChange;

  beforeEach(() => {
    vi.clearAllMocks();
    onStoryDaysChange = vi.fn();
  });

  // Test 1: js-early-exit — returns null when tripId is falsy
  it("returns null when tripId is null", () => {
    useTripDetails.mockReturnValue({ trip: null, loading: false });

    const { container } = render(
      <TripItineraryEditor
        tripId={null}
        storyDays={[]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );
    expect(container.innerHTML).toBe("");
  });

  // Test 2: rendering-conditional-render — loading spinner
  it("shows loading spinner while trip data loads", () => {
    useTripDetails.mockReturnValue({ trip: null, loading: true });

    const { container } = render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );
    // Should render a spinner (animate-spin class), not the empty state text
    const spinner = container.querySelector(".animate-spin");
    expect(spinner).not.toBeNull();
  });

  // Test 3: rerender-derived-state-no-effect — groups by day
  it("renders day headers grouped correctly", () => {
    useTripDetails.mockReturnValue({ trip: mockTrip, loading: false });

    render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[
          {
            dayNumber: 1,
            title: "Ngày 1",
            content: "",
            destinationName: "Bến Nhà Rồng",
            displayOrder: 0,
          },
          {
            dayNumber: 1,
            title: "Ngày 1",
            content: "",
            destinationName: "Chợ Bến Thành",
            displayOrder: 1,
          },
          {
            dayNumber: 2,
            title: "Ngày 2",
            content: "",
            destinationName: "Cầu Rồng",
            displayOrder: 2,
          },
        ]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );

    expect(screen.getByText(/Ngày 1/)).toBeDefined();
    expect(screen.getByText(/Ngày 2/)).toBeDefined();
  });

  // Test 4: rendering-hoist-jsx — location name labels
  it("renders textarea per location with location name", () => {
    useTripDetails.mockReturnValue({ trip: mockTrip, loading: false });

    render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[
          {
            dayNumber: 1,
            content: "",
            destinationName: "Bến Nhà Rồng",
            displayOrder: 0,
          },
          {
            dayNumber: 1,
            content: "",
            destinationName: "Chợ Bến Thành",
            displayOrder: 1,
          },
          {
            dayNumber: 2,
            content: "",
            destinationName: "Cầu Rồng",
            displayOrder: 2,
          },
        ]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );

    expect(screen.getByText("Bến Nhà Rồng")).toBeDefined();
    expect(screen.getByText("Chợ Bến Thành")).toBeDefined();
    expect(screen.getByText("Cầu Rồng")).toBeDefined();

    // Check textareas exist
    const textareas = screen.getAllByRole("textbox");
    expect(textareas.length).toBe(3);
  });

  // Test 5: rerender-move-effect-to-event — content change handler
  it("calls onStoryDaysChange when textarea content changes", () => {
    useTripDetails.mockReturnValue({ trip: mockTrip, loading: false });

    render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[
          {
            dayNumber: 1,
            content: "",
            destinationName: "Bến Nhà Rồng",
            displayOrder: 0,
          },
          {
            dayNumber: 1,
            content: "",
            destinationName: "Chợ Bến Thành",
            displayOrder: 1,
          },
          {
            dayNumber: 2,
            content: "",
            destinationName: "Cầu Rồng",
            displayOrder: 2,
          },
        ]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );

    const textareas = screen.getAllByRole("textbox");
    fireEvent.change(textareas[0], {
      target: { value: "Trải nghiệm tuyệt vời!" },
    });

    expect(onStoryDaysChange).toHaveBeenCalled();
  });

  // Test 6: character count display
  it("shows character count per textarea", () => {
    useTripDetails.mockReturnValue({ trip: mockTrip, loading: false });

    render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[
          {
            dayNumber: 1,
            content: "Hello",
            destinationName: "Bến Nhà Rồng",
            displayOrder: 0,
          },
          {
            dayNumber: 1,
            content: "",
            destinationName: "Chợ Bến Thành",
            displayOrder: 1,
          },
          {
            dayNumber: 2,
            content: "",
            destinationName: "Cầu Rồng",
            displayOrder: 2,
          },
        ]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );

    // Should show "5/500" for first entry that has "Hello"
    expect(screen.getByText("5/500")).toBeDefined();
  });

  // Test 7: auto-initialization for create mode
  it("initializes storyDays from trip data when empty", () => {
    useTripDetails.mockReturnValue({ trip: mockTrip, loading: false });

    render(
      <TripItineraryEditor
        tripId="trip-1"
        storyDays={[]}
        onStoryDaysChange={onStoryDaysChange}
      />,
    );

    // onStoryDaysChange should be called with initialized array
    expect(onStoryDaysChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          dayNumber: 1,
          destinationName: "Bến Nhà Rồng",
          content: "",
          displayOrder: 0,
        }),
        expect.objectContaining({
          dayNumber: 1,
          destinationName: "Chợ Bến Thành",
          content: "",
          displayOrder: 1,
        }),
        expect.objectContaining({
          dayNumber: 2,
          destinationName: "Cầu Rồng",
          content: "",
          displayOrder: 2,
        }),
      ]),
    );
  });
});
