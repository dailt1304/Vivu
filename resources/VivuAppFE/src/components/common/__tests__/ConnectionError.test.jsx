import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import ConnectionError from "../ConnectionError";

describe("ConnectionError", () => {
  it("should render default message when no message prop is provided", () => {
    render(<ConnectionError />);

    expect(screen.getByText("Không thể kết nối")).toBeInTheDocument();
    expect(
      screen.getByText(
        "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng kiểm tra kết nối mạng và thử lại.",
      ),
    ).toBeInTheDocument();
  });

  it("should render custom message when provided", () => {
    const customMessage = "Lỗi máy chủ 500";
    render(<ConnectionError message={customMessage} />);

    expect(screen.getByText("Không thể kết nối")).toBeInTheDocument();
    expect(screen.getByText(customMessage)).toBeInTheDocument();
  });

  it("should call onRetry callback when button is clicked", () => {
    const mockOnRetry = vi.fn();
    render(<ConnectionError onRetry={mockOnRetry} />);

    const retryButton = screen.getByRole("button", { name: /Thử lại/i });
    fireEvent.click(retryButton);

    expect(mockOnRetry).toHaveBeenCalledTimes(1);
  });

  it("should display loading state and disable button when isRetrying is true", () => {
    const mockOnRetry = vi.fn();
    render(<ConnectionError onRetry={mockOnRetry} isRetrying={true} />);

    const retryButton = screen.getByRole("button", { name: /Đang thử lại/i });
    expect(retryButton).toBeDisabled();

    fireEvent.click(retryButton);
    expect(mockOnRetry).not.toHaveBeenCalled();
  });
});
