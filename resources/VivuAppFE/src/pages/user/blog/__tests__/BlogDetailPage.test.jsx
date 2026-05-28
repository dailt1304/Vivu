import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import React, { Suspense } from "react";
import { BrowserRouter } from "react-router-dom";
import BlogDetailPage from "../BlogDetailPage";
import * as useBlogsHook from "../../../../hooks/blogs/useBlogs";
import { useAuth } from "../../../../contexts/auth-context";

// Mock hooks
vi.mock("../../../../hooks/blogs/useBlogs");
vi.mock("../../../../contexts/auth-context", () => ({
  useAuth: vi.fn(),
}));
vi.mock("../../../../utils/toast", () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

// Mock Router
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useParams: () => ({ id: "123" }),
    useNavigate: () => vi.fn(),
  };
});

// Mock MapContainer
vi.mock("../../../../components/common/map/MapContainer", () => ({
  default: ({ locations, onMarkerClick }) => (
    <div data-testid="mock-map">
      {locations.map((loc) => (
        <button
          key={loc.id}
          data-testid={`marker-${loc.id}`}
          onClick={() => onMarkerClick(loc)}
        >
          {loc.title}
        </button>
      ))}
    </div>
  ),
}));

// Mock DestinationDrawer
vi.mock("../../../../components/common/drawers/DestinationDrawer", () => ({
  default: ({ isOpen, data }) =>
    isOpen ? (
      <div data-testid="mock-drawer">Drawer for {data?.name}</div>
    ) : null,
}));

const mockBlogData = {
  id: "123",
  title: "Test Blog Title",
  shortDescription: "This is a short description.",
  content: "<p>HTML Content</p>",
  userId: "user-1",
  authorName: "Test User",
  blogStoryDays: [
    { dayNumber: 1, title: "Ngày 1: Khám phá", content: "Nội dung ngày 1" },
    { dayNumber: 2, title: "Ngày 2: Ăn uống", content: "Nội dung ngày 2" },
  ],
  tripLocations: [
    {
      id: "loc-1",
      dayNumber: 1,
      orderIndex: 1,
      startTime: "08:00:00",
      endTime: "10:00:00",
      transportMode: "taxi",
      note: "Ghi chú 1",
      location: {
        id: "l-1",
        name: "Dinh Độc Lập",
        address: "135 Nam Kỳ",
        latitude: 10,
        longitude: 106,
      },
    },
    {
      id: "loc-2",
      dayNumber: 1,
      orderIndex: 2,
      startTime: "10:30:00",
      transportMode: "walking",
      location: {
        id: "l-2",
        name: "Nhà thờ Đức Bà",
        address: "Paris",
        latitude: 10.1,
        longitude: 106.1,
      },
    },
  ],
};

describe("BlogDetailPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuth.mockReturnValue({ user: { id: "user-2" }, userId: "user-2" }); // Not owner
    useBlogsHook.useDeleteBlog.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: mockBlogData,
      isLoading: false,
      error: null,
    });
    useBlogsHook.useMyBookmarks.mockReturnValue({
      data: { items: [] },
      mutate: vi.fn(),
    });
    useBlogsHook.useLikeBlog.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
    useBlogsHook.useBookmarkBlog.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
    useBlogsHook.useBlogComments.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    });
    useBlogsHook.useCreateComment.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
    useBlogsHook.useDeleteComment.mockReturnValue({
      trigger: vi.fn(),
      isMutating: false,
    });
  });

  const TestWrapper = () => (
    <BrowserRouter>
      <Suspense fallback={<div>Loading...</div>}>
        <BlogDetailPage />
      </Suspense>
    </BrowserRouter>
  );

  it("renders loading state", () => {
    useBlogsHook.useBlogDetail.mockReturnValue({ data: null, isLoading: true });
    render(<TestWrapper />);
    expect(document.querySelector(".animate-spin")).toBeInTheDocument();
  });

  it("renders empty/error state", () => {
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: null,
      error: new Error(),
      isLoading: false,
    });
    render(<TestWrapper />);
    expect(screen.getByText(/Blog không được tìm thấy/i)).toBeInTheDocument();
  });

  it("renders day headers from itinerary and locations", async () => {
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: mockBlogData,
      isLoading: false,
    });
    render(<TestWrapper />);

    // Verify Title and Description
    expect(screen.getByText("Test Blog Title")).toBeInTheDocument();
    expect(
      screen.getByText("This is a short description."),
    ).toBeInTheDocument();

    // Verify Days
    expect(screen.getByText("Ngày 1: Khám phá")).toBeInTheDocument();
    expect(screen.getByText("Nội dung ngày 1")).toBeInTheDocument();

    // Verify Locations
    expect(screen.getAllByText("Dinh Độc Lập").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Nhà thờ Đức Bà").length).toBeGreaterThan(0);

    // Verify Time and Notes
    expect(screen.getByText("08:00 - 10:00")).toBeInTheDocument();
    expect(screen.getAllByText(/10:30/).length).toBeGreaterThan(0); // endTime is missing
    expect(screen.getByText("Ghi chú 1")).toBeInTheDocument();
  });

  it("derives allMapLocations correctly and renders map makers", () => {
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: mockBlogData,
      isLoading: false,
    });
    render(<TestWrapper />);

    expect(screen.getByTestId("mock-map")).toBeInTheDocument();
    expect(screen.getByTestId("marker-l-1")).toBeInTheDocument();
    expect(screen.getByTestId("marker-l-2")).toBeInTheDocument();
  });

  it("opens DestinationDrawer with real location data when 'Xem chi tiết' is clicked", async () => {
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: mockBlogData,
      isLoading: false,
    });
    render(<TestWrapper />);

    const detailsButtons = screen.getAllByText("Xem chi tiết");
    fireEvent.click(detailsButtons[0]);

    // The drawer should open with the selected location name
    await waitFor(() => {
      expect(screen.getByTestId("mock-drawer")).toHaveTextContent(
        "Drawer for Dinh Độc Lập",
      );
    });
  });

  it("handles empty tripLocations gracefully", () => {
    const emptyData = { ...mockBlogData, tripLocations: [] };
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: emptyData,
      isLoading: false,
    });
    render(<TestWrapper />);

    expect(
      screen.getByText("Chưa có lịch trình cho ngày này."),
    ).toBeInTheDocument();
  });

  it("displays shortDescription instead of hardcoded fallback", () => {
    useBlogsHook.useBlogDetail.mockReturnValue({
      data: mockBlogData,
      isLoading: false,
    });
    render(<TestWrapper />);

    expect(
      screen.getByText("This is a short description."),
    ).toBeInTheDocument();
    expect(screen.queryByText("Lorem ipsum")).not.toBeInTheDocument();
  });
});
