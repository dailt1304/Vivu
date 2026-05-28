import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import LocationListTab from "../LocationListTab";
import locationApi from "@/api/locationApi";
import { SWRConfig } from "swr";
import React from "react";

// Mock the API using vitest
vi.mock("@/api/locationApi", () => ({
  default: {
    getAll: vi.fn(),
  },
}));

// Mock child components to isolate LocationListTab
vi.mock("@/components/cms/locations/LocationEditSheet", () => ({
  default: ({ open, onOpenChange, location, onMutate }) =>
    open ? (
      <div data-testid="location-edit-sheet">
        <span>Edit {location?.name}</span>
        <button onClick={onMutate}>Save</button>
        <button onClick={() => onOpenChange(false)}>Close</button>
      </div>
    ) : null,
}));

const mockData = {
  success: true,
  data: {
    items: [
      {
        id: "1",
        name: "Location 1",
        address: "Address 1",
        ratingAverage: 4.5,
        isVerified: true,
        categoryId: 1,
        locationDetail: {
          openingHours: "08:00 - 22:00",
          phone: "0123456789",
          website: "https://loc1.com",
          tags: "tag1, tag2",
          images: "https://img1.jpg, https://img2.jpg",
        },
      },
      {
        id: "2",
        name: "Location 2",
        address: "Address 2",
        ratingAverage: 3.8,
        isVerified: false,
        categoryId: 2,
        locationDetail: {
          openingHours: "09:00 - 18:00",
          phone: "0987654321",
          website: "https://loc2.com",
          tags: "tag3",
          images: "https://img3.jpg",
        },
      },
    ],
    totalCount: 2,
    pageNumber: 1,
    pageSize: 200,
    totalPages: 1,
  },
};

describe("LocationListTab", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const renderWithSWR = (ui) => {
    return render(
      <SWRConfig value={{ provider: () => new Map(), dedupingInterval: 0 }}>
        {ui}
      </SWRConfig>,
    );
  };

  it("renders loading state with skeletons", async () => {
    locationApi.getAll.mockReturnValue(new Promise(() => {})); // Never resolves to keep loading

    renderWithSWR(<LocationListTab />);

    // DataTable renders 5 skeletons by default when loading
    // In our DataTable.jsx it uses Skeleton component from @/components/ui/skeleton
    expect(document.querySelector(".animate-pulse")).toBeInTheDocument();
  });

  it("renders data when fetch is successful", async () => {
    locationApi.getAll.mockResolvedValue(mockData);

    renderWithSWR(<LocationListTab />);

    await waitFor(() => {
      expect(screen.getByText("Location 1")).toBeInTheDocument();
      expect(screen.getByText("Location 2")).toBeInTheDocument();
    });

    expect(screen.getByText("Address 1")).toBeInTheDocument();
  });

  it("handles search input and filters locally (no additional API calls)", async () => {
    locationApi.getAll.mockResolvedValue(mockData);

    renderWithSWR(<LocationListTab />);

    await waitFor(() => {
      expect(screen.getByText("Location 1")).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm địa điểm/i);
    fireEvent.change(searchInput, { target: { value: "Location 1" } });

    // Wait for debounce and client-side filter
    await waitFor(() => {
      expect(screen.getByText("Location 1")).toBeInTheDocument();
      expect(screen.queryByText("Location 2")).not.toBeInTheDocument();
    });

    // Verify no additional API calls were made after the initial one
    expect(locationApi.getAll).toHaveBeenCalledTimes(1);
  });

  it("shows correct pagination label for client-side processing", async () => {
    locationApi.getAll.mockResolvedValue(mockData);

    renderWithSWR(<LocationListTab />);

    await waitFor(() => {
      expect(screen.getByText(/Hiển thị 1 - 2 trên 2/i)).toBeInTheDocument();
      expect(screen.getByText(/Trang 1 \/ 1/i)).toBeInTheDocument();
    });
  });

  it("shows error toast when fetch fails", async () => {
    const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
    locationApi.getAll.mockRejectedValue(new Error("API Error"));

    renderWithSWR(<LocationListTab />);

    await waitFor(() => {
      expect(consoleSpy).toHaveBeenCalled();
    });
    consoleSpy.mockRestore();
  });
});
