import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import UserSubmissionsTab from "../UserSubmissionsTab";
import {
  useMySubmissions,
  useSuggestUpdateLocation,
} from "../../../../hooks/locations/useLocations";

// Mock dependencies
vi.mock("../../../../hooks/locations/useLocations", () => ({
  useMySubmissions: vi.fn(),
  useSuggestUpdateLocation: vi.fn(),
}));

vi.mock("framer-motion", () => ({
  motion: {
    div: ({ children, className }) => (
      <div className={className} data-testid="submission-card">
        {children}
      </div>
    ),
  },
  AnimatePresence: ({ children }) => <>{children}</>,
}));

describe("UserSubmissionsTab", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useSuggestUpdateLocation.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
  });

  it("should render empty state when no submissions exist", () => {
    // Mock the hook to return empty array
    useMySubmissions.mockReturnValue({
      data: {
        items: [],
        totalPages: 1,
      },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    render(<UserSubmissionsTab />);

    expect(
      screen.getByText(
        /Bạn chưa gửi địa điểm nào hoặc không có địa điểm nào khớp/i,
      ),
    ).toBeInTheDocument();
  });

  it("should render a list of submissions when data is provided", () => {
    const mockSubmissions = [
      { id: "1", name: "Location 1", status: 1 },
      { id: "2", name: "Location 2", status: 2 },
    ];

    useMySubmissions.mockReturnValue({
      data: {
        items: mockSubmissions,
        totalPages: 1,
      },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    render(<UserSubmissionsTab />);

    expect(screen.getByText("Location 1")).toBeInTheDocument();
    expect(screen.getByText("Location 2")).toBeInTheDocument();

    const cards = screen.getAllByTestId("submission-card");
    expect(cards).toHaveLength(3); // 1 wrapper motion.div + 2 card motion.divs
  });

  it("should render filter buttons and they should be clickable", () => {
    useMySubmissions.mockReturnValue({
      data: {
        items: [],
        totalPages: 1,
      },
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    });

    render(<UserSubmissionsTab />);

    // Just check the buttons exist (the actual filtering logic happens via the hook call
    // in the component, but we are just verifying the UI here)
    const filters = ["Tất cả", "Đang chờ", "Đã duyệt", "Từ chối"];

    filters.forEach((filterLabel) => {
      const btn = screen.getByRole("button", { name: filterLabel });
      expect(btn).toBeInTheDocument();
      fireEvent.click(btn);
    });
  });
});
