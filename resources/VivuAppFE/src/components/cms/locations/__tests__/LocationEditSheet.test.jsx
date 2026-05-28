import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  render,
  screen,
  fireEvent,
  waitFor,
  act,
} from "@testing-library/react";
import LocationEditSheet from "../LocationEditSheet";
import locationCategoryApi from "@/api/locationCategoryApi";
import { SWRConfig } from "swr";
import React from "react";
import * as useLocations from "@/hooks/locations";
import toast from "@/utils/toast";

// Mock hooks
vi.mock("@/hooks/locations", () => ({
  useUpdateLocation: vi.fn(),
  useDeleteLocation: vi.fn(),
  useLocationCategories: vi.fn(),
}));

// Mock category API
vi.mock("@/api/locationCategoryApi", () => ({
  default: {
    getAll: vi.fn(),
  },
}));

// Mock toast
vi.mock("@/utils/toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

// Mock ResizeObserver for Radix UI
globalThis.ResizeObserver = class {
  constructor() {}
  observe() {}
  unobserve() {}
  disconnect() {}
};

// Radix select needs this
globalThis.HTMLElement.prototype.scrollIntoView = vi.fn();
globalThis.HTMLElement.prototype.hasPointerCapture = vi.fn();
globalThis.HTMLElement.prototype.releasePointerCapture = vi.fn();

const mockLocation = {
  id: "loc-1",
  name: "Test Cafe",
  description: "A nice cafe",
  address: "123 Test St",
  latitude: 10.123,
  longitude: 20.456,
  categoryId: 2,
  isVerified: true,
  locationDetail: {
    openingHours: "08:00 - 22:00",
    phone: "0123456789",
    website: "https://testcafe.com",
    tags: "cafe, view",
    images: "https://img1.jpg, https://img2.jpg",
  },
};

const mockCategoriesResponse = {
  items: [
    { id: 1, name: "Restaurant" },
    { id: 2, name: "Cafe" },
  ],
};

describe("LocationEditSheet", () => {
  const mockUpdateTrigger = vi.fn();
  const mockDeleteTrigger = vi.fn();
  const mockOnOpenChange = vi.fn();
  const mockOnMutate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    useLocations.useUpdateLocation.mockReturnValue({
      trigger: mockUpdateTrigger,
      isMutating: false,
    });

    useLocations.useDeleteLocation.mockReturnValue({
      trigger: mockDeleteTrigger,
      isMutating: false,
    });

    useLocations.useLocationCategories.mockReturnValue({
      data: mockCategoriesResponse,
    });

    locationCategoryApi.getAll.mockResolvedValue({
      data: mockCategoriesResponse,
    });

    // Default confirmation to true for delete tests
    vi.stubGlobal("confirm", vi.fn().mockReturnValue(true));
  });

  const renderWithSWR = (ui) => {
    return render(
      <SWRConfig value={{ provider: () => new Map(), dedupingInterval: 0 }}>
        {ui}
      </SWRConfig>,
    );
  };

  it("1. Renders null when no location", () => {
    const { container } = renderWithSWR(
      <LocationEditSheet open={true} location={null} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it("2. Populates all form fields from location data", async () => {
    renderWithSWR(
      <LocationEditSheet
        open={true}
        location={mockLocation}
        onOpenChange={mockOnOpenChange}
      />,
    );

    expect(screen.getByDisplayValue("Test Cafe")).toBeInTheDocument();
    expect(screen.getByDisplayValue("A nice cafe")).toBeInTheDocument();
    expect(screen.getByDisplayValue("123 Test St")).toBeInTheDocument();
    expect(screen.getByDisplayValue("10.123")).toBeInTheDocument();
    expect(screen.getByDisplayValue("20.456")).toBeInTheDocument();
    expect(screen.getByDisplayValue("08:00 - 22:00")).toBeInTheDocument();
    expect(screen.getByDisplayValue("0123456789")).toBeInTheDocument();
    expect(
      screen.getByDisplayValue("https://testcafe.com"),
    ).toBeInTheDocument();
    expect(screen.getByDisplayValue("cafe, view")).toBeInTheDocument();
    // Images are now shown as thumbnails, not in a textarea
    const images = screen.getAllByRole("img");
    expect(images.length).toBe(2);
    expect(images[0]).toHaveAttribute("src", "https://img1.jpg");
    expect(images[1]).toHaveAttribute("src", "https://img2.jpg");
  });

  it("3. Shows validation error for empty name/address", async () => {
    renderWithSWR(
      <LocationEditSheet
        open={true}
        location={{ ...mockLocation, name: "" }}
        onOpenChange={mockOnOpenChange}
      />,
    );

    const saveButton = screen.getByText("Lưu thay đổi").closest("button");
    fireEvent.click(saveButton);

    expect(toast.error).toHaveBeenCalledWith(
      "Vui lòng nhập tên và địa chỉ địa điểm",
    );
    expect(mockUpdateTrigger).not.toHaveBeenCalled();
  });

  it("4. Calls updateLocation with correct payload", async () => {
    renderWithSWR(
      <LocationEditSheet
        open={true}
        location={mockLocation}
        onOpenChange={mockOnOpenChange}
        onMutate={mockOnMutate}
      />,
    );

    // Let's modify the name
    const nameInput = screen.getByDisplayValue("Test Cafe");
    fireEvent.change(nameInput, {
      target: { name: "name", value: "New Cafe Name" },
    });

    const saveButton = screen.getByText("Lưu thay đổi").closest("button");
    act(() => {
      fireEvent.click(saveButton);
    });

    await waitFor(() => {
      expect(mockUpdateTrigger).toHaveBeenCalledTimes(1);
    });

    const calledArg = mockUpdateTrigger.mock.calls[0][0];
    expect(calledArg.id).toBe("loc-1");
    expect(calledArg.data.name).toBe("New Cafe Name");
    expect(calledArg.data.openingHours).toBe("08:00 - 22:00");
    expect(calledArg.data.isVerified).toBe(true);
    // categoryId comes from string 2 when initialized
    expect(calledArg.data.categoryId).toBe("2");

    expect(toast.success).toHaveBeenCalledWith("Cập nhật địa điểm thành công");
    expect(mockOnMutate).toHaveBeenCalled();
    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });

  it("5. Renders category dropdown with options", async () => {
    renderWithSWR(<LocationEditSheet open={true} location={mockLocation} />);

    // Radix UI select trigger has role="combobox"
    const combobox = screen.getByRole("combobox");

    // Open the dropdown
    fireEvent.click(combobox);

    await waitFor(() => {
      expect(
        screen.getByRole("option", { name: "Restaurant" }),
      ).toBeInTheDocument();
      expect(screen.getByRole("option", { name: "Cafe" })).toBeInTheDocument();
    });
  });

  it("6. Toggles isVerified switch", async () => {
    renderWithSWR(
      <LocationEditSheet
        open={true}
        location={{ ...mockLocation, isVerified: false }}
      />,
    );

    const switchBtn = screen.getByRole("switch");
    expect(switchBtn).toHaveAttribute("aria-checked", "false");

    fireEvent.click(switchBtn);

    await waitFor(() => {
      expect(switchBtn).toHaveAttribute("aria-checked", "true");
    });
  });

  it("7. Displays image preview grid", async () => {
    renderWithSWR(<LocationEditSheet open={true} location={mockLocation} />);

    const images = screen.getAllByRole("img");
    // Should be 2 images from the mockLocation link
    expect(images.length).toBe(2);
    expect(images[0]).toHaveAttribute("src", "https://img1.jpg");
    expect(images[1]).toHaveAttribute("src", "https://img2.jpg");
  });

  it("8. Calls deleteLocation on delete confirm", async () => {
    renderWithSWR(
      <LocationEditSheet
        open={true}
        location={mockLocation}
        onOpenChange={mockOnOpenChange}
        onMutate={mockOnMutate}
      />,
    );

    const deleteButton = screen.getByText("Xóa").closest("button");
    fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(mockDeleteTrigger).toHaveBeenCalledWith("loc-1");
    });

    expect(toast.success).toHaveBeenCalledWith("Đã xóa địa điểm");
    expect(mockOnMutate).toHaveBeenCalled();
    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });
});
