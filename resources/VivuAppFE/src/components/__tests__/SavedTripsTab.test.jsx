import { describe, it, expect, beforeEach, vi } from "vitest";
import { render, screen, waitFor } from "../../test/utils";
import userEvent from "@testing-library/user-event";
import SavedTripsTab from "../user/profile/SavedTripsTab";
import userApi from "../../api/userApi";

// Mock the API module directly to easily control SWR fetcher states
vi.mock("../../api/userApi", () => ({
  default: {
    getMyFavoriteTrips: vi.fn(),
  },
}));

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("SavedTripsTab", () => {
  const user = userEvent.setup();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should render loading state initially", () => {
    // Return a never-resolving promise to stick in loading state
    userApi.getMyFavoriteTrips.mockReturnValue(new Promise(() => {}));

    // Custom render wraps it in MemoryRouter and AuthProvider+SWRConfig
    const { container } = render(<SavedTripsTab />);

    // Query the loader element
    const loader = container.querySelector(".animate-spin");
    expect(loader).toBeInTheDocument();
  });

  it("should render error message on API failure", async () => {
    userApi.getMyFavoriteTrips.mockResolvedValue({
      success: false,
      message: "Server Error",
    });

    render(<SavedTripsTab />);

    await waitFor(() => {
      expect(
        screen.getByText("Không thể tải danh sách chuyến đi đã lưu."),
      ).toBeInTheDocument();
    });
  });

  it("should render empty state when no favorite trips exist", async () => {
    userApi.getMyFavoriteTrips.mockResolvedValue({
      success: true,
      data: [],
    });

    render(<SavedTripsTab />);

    await waitFor(() => {
      expect(
        screen.getByText("Chưa có chuyến đi nào được lưu"),
      ).toBeInTheDocument();
    });

    // Test the "Explore Now" button
    const exploreBtn = screen.getByRole("button", { name: "Khám phá ngay" });
    await user.click(exploreBtn);
    expect(mockNavigate).toHaveBeenCalledWith("/explore");
  });

  it("should render list of favorite trips when data exists", async () => {
    const mockData = [
      {
        id: "trip-1",
        title: "Trip A",
        startDate: "2026-03-01T00:00:00",
        coverUrl: "http://example.com/a.jpg",
        tripDays: [{ locations: [1] }, { locations: [1, 2] }], // 3 locations total
      },
      {
        id: "trip-2",
        // Missing title defaults to "Chuyến đi không tên"
        tripDays: [],
      },
    ];

    userApi.getMyFavoriteTrips.mockResolvedValue({
      success: true,
      data: mockData,
    });

    render(<SavedTripsTab />);

    await waitFor(() => {
      expect(screen.getByText("Trip A")).toBeInTheDocument();
      expect(screen.getByText("Chuyến đi không tên")).toBeInTheDocument();
    });

    // Check location count derived from tripDays
    expect(screen.getByText("3 Places")).toBeInTheDocument();
    expect(screen.getByText("0 Places")).toBeInTheDocument();

    // Check map navigation
    const tripACard = screen.getByText("Trip A").closest("div.group");
    await user.click(tripACard);
    expect(mockNavigate).toHaveBeenCalledWith("/trips/trip-1");
  });
});
