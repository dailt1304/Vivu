import { describe, it, expect, vi, beforeEach } from "vitest";
import * as fs from "fs";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import ReportLocationModal from "../ReportLocationModal";
import { useReportLocation } from "../../../../hooks/locations/useLocations";

// Mock dependencies
vi.mock("../../../../hooks/locations/useLocations", () => ({
  useReportLocation: vi.fn(),
}));

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

describe("ReportLocationModal", () => {
  const mockOnClose = vi.fn();
  const mockReportLocation = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useReportLocation.mockReturnValue({
      trigger: mockReportLocation,
      isMutating: false,
    });
  });

  it("should not render anything when isOpen is false", () => {
    const { container } = render(
      <ReportLocationModal
        isOpen={false}
        onClose={mockOnClose}
        locationId="1"
        locationName="Test Loc"
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("should render form and location name when isOpen is true", () => {
    render(
      <ReportLocationModal
        isOpen={true}
        onClose={mockOnClose}
        locationId="1"
        locationName="Test Location 123"
      />,
    );

    expect(screen.getByText("Báo cáo địa điểm")).toBeInTheDocument();
    expect(screen.getByText("Test Location 123")).toBeInTheDocument();
    expect(
      screen.getByText(/Thông tin không chính xác/i).closest("label"),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Địa điểm đã đóng cửa/i).closest("label"),
    ).toBeInTheDocument();
    expect(screen.getByText(/Đề xuất địa điểm mới/i)).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText(/Cung cấp thêm thông tin/i),
    ).toBeInTheDocument();
  });

  it("should call reportLocation API when form is submitted", async () => {
    mockReportLocation.mockResolvedValueOnce({ success: true });

    const { container } = render(
      <ReportLocationModal
        isOpen={true}
        onClose={mockOnClose}
        locationId="loc-123"
        locationName="Test Loc"
      />,
    );

    // Select a report type (e.g., Type 2: "Địa điểm đã đóng cửa")
    const radio = screen.getByRole("radio", {
      name: /Địa điểm đã đóng cửa/i,
    });
    fireEvent.click(radio);

    // Enter a reason
    const reasonInput = screen.getByPlaceholderText(
      /VD: Định vị bị sai lệch 500m/i,
    );
    fireEvent.change(reasonInput, {
      target: { value: "Địa điểm đã đóng cửa" },
    });

    // Enter a description
    const textarea = screen.getByPlaceholderText(/Cung cấp thêm thông tin/i);
    fireEvent.change(textarea, {
      target: { value: "It is permanently closed." },
    });

    // Click submit
    const form = container.querySelector("form");
    fireEvent.submit(form);

    fs.writeFileSync("output.html", container.innerHTML);

    await waitFor(() => {
      expect(mockReportLocation).toHaveBeenCalledWith({
        locationId: "loc-123",
        reportType: 2,
        reason: "Địa điểm đã đóng cửa",
        description: "It is permanently closed.",
      });
      // The modal should close after successful report
      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
  });
});
