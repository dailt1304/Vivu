import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import LocationCard from "../LocationCard";

describe("LocationCard", () => {
  const mockProps = {
    title: "Cầu Rồng",
    location: "Đà Nẵng",
    rating: 4.8,
    reviews: 120,
    category: "Cầu",
    image: "test.jpg",
    onClick: vi.fn(),
    onSave: vi.fn(),
    onAddToTrip: vi.fn(),
  };

  it("should render title and location", () => {
    render(<LocationCard {...mockProps} />);
    expect(screen.getByText("Cầu Rồng")).toBeInTheDocument();
    expect(screen.getByText("Đà Nẵng")).toBeInTheDocument();
  });

  it("should render rating and reviews", () => {
    render(<LocationCard {...mockProps} />);
    expect(screen.getByText("4.8")).toBeInTheDocument();
    expect(screen.getByText(/120 đánh giá/i)).toBeInTheDocument();
  });

  it("should call onClick when the card is clicked", () => {
    render(<LocationCard {...mockProps} />);
    const card = screen.getByText("Cầu Rồng").closest('div[class*="group"]');
    fireEvent.click(card);
    expect(mockProps.onClick).toHaveBeenCalled();
  });

  it("should call onAddToTrip when plus button is clicked", () => {
    render(<LocationCard {...mockProps} />);
    const addButton = screen.getByTitle(/Thêm vào chuyến đi/i);
    fireEvent.click(addButton);
    expect(mockProps.onAddToTrip).toHaveBeenCalled();
  });
});
