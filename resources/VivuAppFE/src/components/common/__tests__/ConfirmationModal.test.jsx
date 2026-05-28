import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import ConfirmationModal from "../modals/ConfirmationModal";

// Mock framer-motion to bypass animations
vi.mock("framer-motion", () => ({
  motion: {
    div: ({ children, className, onClick }) => (
      <div className={className} onClick={onClick} data-testid="motion-div">
        {children}
      </div>
    ),
  },
  AnimatePresence: ({ children }) => <>{children}</>,
}));

describe("ConfirmationModal", () => {
  const mockOnClose = vi.fn();
  const mockOnConfirm = vi.fn();

  it("should not render when isOpen is false", () => {
    const { container } = render(
      <ConfirmationModal
        isOpen={false}
        onClose={mockOnClose}
        onConfirm={mockOnConfirm}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("should render title and message when isOpen is true", () => {
    render(
      <ConfirmationModal
        isOpen={true}
        onClose={mockOnClose}
        onConfirm={mockOnConfirm}
        title="Custom Title"
        message="Custom Message"
        confirmText="Yes"
        cancelText="No"
      />,
    );

    expect(screen.getByText("Custom Title")).toBeInTheDocument();
    expect(screen.getByText("Custom Message")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Yes" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "No" })).toBeInTheDocument();
  });

  it("should call onClose when cancel button is clicked", () => {
    render(
      <ConfirmationModal
        isOpen={true}
        onClose={mockOnClose}
        onConfirm={mockOnConfirm}
      />,
    );

    const cancelButton = screen.getByRole("button", { name: /Hủy/i });
    fireEvent.click(cancelButton);

    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it("should call onConfirm when confirm button is clicked", () => {
    render(
      <ConfirmationModal
        isOpen={true}
        onClose={mockOnClose}
        onConfirm={mockOnConfirm}
      />,
    );

    const confirmButton = screen.getByRole("button", { name: /Xóa/i });
    fireEvent.click(confirmButton);

    expect(mockOnConfirm).toHaveBeenCalledTimes(1);
  });
});
