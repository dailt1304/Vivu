import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter, useRouteError, useNavigate } from "react-router-dom";
import ErrorBoundary from "../ErrorBoundary";

// Mock react-router-dom hooks
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useRouteError: vi.fn(),
    useNavigate: vi.fn(),
  };
});

describe("ErrorBoundary", () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useNavigate.mockReturnValue(mockNavigate);

    // Mock window.location.reload
    Object.defineProperty(window, "location", {
      writable: true,
      value: { reload: vi.fn() },
    });
  });

  const renderWithRouter = () => {
    return render(
      <MemoryRouter>
        <ErrorBoundary />
      </MemoryRouter>,
    );
  };

  it("should render 404 message when status is 404", () => {
    useRouteError.mockReturnValue({ status: 404, statusText: "Not Found" });
    renderWithRouter();

    expect(screen.getByText("404")).toBeInTheDocument();
    expect(screen.getByText("Không tìm thấy trang")).toBeInTheDocument();
    expect(
      screen.getByText(
        "Có vẻ như trang bạn tìm kiếm không tồn tại hoặc đã bị xóa.",
      ),
    ).toBeInTheDocument();
  });

  it("should render generic error message for other errors", () => {
    useRouteError.mockReturnValue({ status: 500, message: "Server Error" });
    renderWithRouter();

    expect(screen.getByText("500")).toBeInTheDocument();
    expect(screen.getByText("Đã có lỗi xảy ra")).toBeInTheDocument();
    expect(screen.getByText("Server Error")).toBeInTheDocument();
  });

  it("should navigate to home when Trang chủ button is clicked", () => {
    useRouteError.mockReturnValue({});
    renderWithRouter();

    const homeButton = screen.getByRole("button", { name: /Trang chủ/i });
    fireEvent.click(homeButton);

    expect(mockNavigate).toHaveBeenCalledWith("/");
  });

  it("should reload page when Tải lại button is clicked", () => {
    useRouteError.mockReturnValue({});
    renderWithRouter();

    const reloadButton = screen.getByRole("button", { name: /Tải lại/i });
    fireEvent.click(reloadButton);

    expect(window.location.reload).toHaveBeenCalledTimes(1);
  });
});
