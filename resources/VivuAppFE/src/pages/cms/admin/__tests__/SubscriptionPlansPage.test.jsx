import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { vi, describe, beforeEach, it, expect } from "vitest";
import SubscriptionPlansPage from "../SubscriptionPlansPage";
import {
  useSubscriptionPlans,
  useSubscriptionMutation,
} from "@/hooks/subscriptions/useSubscriptions";

// Mock the hooks
vi.mock("@/hooks/subscriptions/useSubscriptions", () => ({
  useSubscriptionPlans: vi.fn(),
  useSubscriptionMutation: vi.fn(),
}));

// Mock Sonner toast
vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

const mockPlans = [
  {
    id: "1",
    name: "Free Plan",
    price: 0,
    duration: "Vĩnh viễn",
    description: "Basic features",
    features: ["Feature 1"],
    isActive: true,
    isFeatured: false,
    color: "gray",
  },
];

describe("SubscriptionPlansPage", () => {
  const mockMutate = vi.fn();
  const mockCreatePlan = vi.fn();
  const mockUpdatePlan = vi.fn();
  const mockDeletePlan = vi.fn();

  beforeEach(() => {
    useSubscriptionPlans.mockReturnValue({
      plans: mockPlans,
      isLoading: false,
      mutate: mockMutate,
    });

    useSubscriptionMutation.mockReturnValue({
      createPlan: mockCreatePlan,
      updatePlan: mockUpdatePlan,
      deletePlan: mockDeletePlan,
      isCreating: false,
      isUpdating: false,
    });
  });

  it("renders existing subscription plans", async () => {
    render(<SubscriptionPlansPage />);
    expect(await screen.findByText("Free Plan")).toBeDefined();
    expect(await screen.findByText("Basic features")).toBeDefined();
  });

  it('opens add plan dialog when clicking "Thêm gói mới"', async () => {
    render(<SubscriptionPlansPage />);
    const addButton = screen.getByText("Thêm gói mới");
    fireEvent.click(addButton);
    expect(await screen.findByText("Thêm gói đăng ký mới")).toBeDefined();
  });

  it("calls createPlan mutation when saving a new plan", async () => {
    mockCreatePlan.mockResolvedValueOnce({});
    render(<SubscriptionPlansPage />);

    fireEvent.click(screen.getByText("Thêm gói mới"));

    const nameInput = await screen.findByPlaceholderText("VD: Premium Gold");
    fireEvent.change(nameInput, { target: { value: "New Pro Plan" } });

    const saveButton = screen.getByText("Lưu");
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(mockCreatePlan).toHaveBeenCalled();
      expect(mockMutate).toHaveBeenCalled();
    });
  });

  it("calls deletePlan mutation when deleting a plan", async () => {
    vi.spyOn(window, "confirm").mockReturnValue(true);
    mockDeletePlan.mockResolvedValueOnce({});

    render(<SubscriptionPlansPage />);

    const trigger = await screen.findByTestId("plan-actions-trigger");
    fireEvent.click(trigger);

    const deleteButton = await screen.findByText("Xóa gói");
    fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(mockDeletePlan).toHaveBeenCalledWith("1");
      expect(mockMutate).toHaveBeenCalled();
    });
  });
});
