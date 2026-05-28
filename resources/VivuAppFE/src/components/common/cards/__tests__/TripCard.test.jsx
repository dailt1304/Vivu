import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import TripCard from "../TripCard";

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("TripCard", () => {
  const mockTrip = {
    id: "123",
    title: "Awesome Trip",
    coverUrl: "http://example.com/image.jpg",
    cityName: "Hanoi",
    durationDays: 3,
    memberCount: 4,
    favoritesCount: 10,
    ownerName: "Alice",
    ownerAvatar: "http://example.com/alice.jpg",
    date: "10/10/2023",
    description: "Trip desc",
  };

  const renderWithRouter = (ui) => {
    return render(<BrowserRouter>{ui}</BrowserRouter>);
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should not render if trip prop is absent", () => {
    const { container } = renderWithRouter(<TripCard />);
    expect(container.firstChild).toBeNull();
  });

  it("should render correctly with given trip data", () => {
    renderWithRouter(<TripCard trip={mockTrip} />);

    expect(screen.getByText("Awesome Trip")).toBeInTheDocument();
    expect(screen.getByText("Hanoi")).toBeInTheDocument();
    expect(screen.getByText("3 ngày")).toBeInTheDocument();
    expect(screen.getByText("4 tv")).toBeInTheDocument();
    expect(screen.getByText("10")).toBeInTheDocument(); // favorites
    expect(screen.getByText("Alice")).toBeInTheDocument();
    expect(screen.queryByText("Trip desc")).not.toBeInTheDocument();
  });

  it("should render description when large prop is true", () => {
    renderWithRouter(<TripCard trip={mockTrip} large />);
    expect(screen.getByText("Trip desc")).toBeInTheDocument();
  });

  it("should navigate to trip detail on click", () => {
    renderWithRouter(<TripCard trip={mockTrip} />);

    // The closest div that responds to onClick. We can grab the title and click
    fireEvent.click(screen.getByText("Awesome Trip"));
    expect(mockNavigate).toHaveBeenCalledWith("/trips/public/123");
  });

  it("should not navigate when clicking the interactive heart button", () => {
    renderWithRouter(<TripCard trip={mockTrip} />);

    const heartBtn = screen.getByRole("button", { name: /Favorite Trip/i });
    fireEvent.click(heartBtn);

    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
