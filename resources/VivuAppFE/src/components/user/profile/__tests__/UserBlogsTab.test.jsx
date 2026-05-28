import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router-dom";
import UserBlogsTab from "../UserBlogsTab";

// Mock framer-motion
import * as useBlogsHook from "../../../hooks/blogs/useBlogs";

vi.mock("../../../hooks/blogs/useBlogs");
vi.mock("framer-motion", () => ({
  motion: {
    div: ({ children, onClick, className }) => (
      <div onClick={onClick} className={className} data-testid="blog-card">
        {children}
      </div>
    ),
  },
}));

// Mock react-router-dom
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

describe("UserBlogsTab", () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useNavigate.mockReturnValue(mockNavigate);
    useBlogsHook.useMyBlogs.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    });
  });

  const renderWithRouter = (ui) => {
    return render(<MemoryRouter>{ui}</MemoryRouter>);
  };

  it("should render empty state when blogs array is empty", () => {
    useBlogsHook.useMyBlogs.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    });
    renderWithRouter(<UserBlogsTab isOwnProfile={true} />);

    expect(screen.getByText("Chưa có bài viết nào")).toBeInTheDocument();
  });

  it("should render list of blogs when data is provided", () => {
    const mockBlogs = [
      { id: 1, title: "Blog 1", likeCount: 10, viewCount: 100 },
      { id: 2, title: "Blog 2", likeCount: 5, viewCount: 50 },
    ];
    useBlogsHook.useMyBlogs.mockReturnValue({
      data: { items: mockBlogs },
      isLoading: false,
    });

    renderWithRouter(<UserBlogsTab isOwnProfile={true} />);

    expect(screen.queryByText("Chưa có bài viết nào")).not.toBeInTheDocument();
    expect(screen.getByText("Blog 1")).toBeInTheDocument();
    expect(screen.getByText("Blog 2")).toBeInTheDocument();
    // Stats summary
    expect(screen.getByText("2")).toBeInTheDocument(); // total blogs
    expect(screen.getByText("15")).toBeInTheDocument(); // total likes
    expect(screen.getByText("150")).toBeInTheDocument(); // total views
  });

  it("should show 'Viết bài mới' button only when isOwnProfile is true", () => {
    useBlogsHook.useMyBlogs.mockReturnValue({
      data: { items: [] },
      isLoading: false,
    });
    const { rerender } = renderWithRouter(<UserBlogsTab isOwnProfile={true} />);

    expect(
      screen.getByRole("button", { name: /Viết bài đầu tiên/i }),
    ).toBeInTheDocument();

    rerender(
      <MemoryRouter>
        <UserBlogsTab isOwnProfile={false} />
      </MemoryRouter>,
    );
    expect(
      screen.queryByRole("button", { name: /Viết bài/i }),
    ).not.toBeInTheDocument();
  });
});
