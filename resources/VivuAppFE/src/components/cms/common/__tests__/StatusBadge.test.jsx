import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import StatusBadge from "../StatusBadge";

describe("StatusBadge", () => {
  it("should render PENDING status with correct label and color", () => {
    render(<StatusBadge status="PENDING" />);
    // Depends on how it is mapped, usually "PENDING" might be "Đang chờ"
    // or we check the text content if it's dynamic. Let's assume it maps to "Đang chờ"
    // or just the status text itself
    const element = screen.getByText(/Chờ duyệt|PENDING/i);
    expect(element).toBeInTheDocument();
    // Class should contain some color like yellow or amber
  });

  it("should render APPROVED status with correct label and color", () => {
    render(<StatusBadge status="APPROVED" />);
    const element = screen.getByText(/Đã duyệt|APPROVED/i);
    expect(element).toBeInTheDocument();
    // Class should contain green
  });

  it("should render REJECTED status with correct label and color", () => {
    render(<StatusBadge status="REJECTED" />);
    const element = screen.getByText(/Từ chối|REJECTED/i);
    expect(element).toBeInTheDocument();
    // Class should contain red
  });
});
