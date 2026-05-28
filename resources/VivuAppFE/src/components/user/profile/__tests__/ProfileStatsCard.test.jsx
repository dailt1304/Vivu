import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import ProfileStatsCard from "../ProfileStatsCard";
import { User } from "lucide-react";

// Mock framer-motion to simplify testing
vi.mock("framer-motion", () => ({
  motion: {
    div: ({ children, onClick, className }) => (
      <div onClick={onClick} className={className} data-testid="motion-div">
        {children}
      </div>
    ),
  },
}));

describe("ProfileStatsCard", () => {
  it("should render value (number/string) and label", () => {
    render(<ProfileStatsCard icon={User} value={42} label="Test Label" />);

    expect(screen.getByText("42")).toBeInTheDocument();
    expect(screen.getByText("Test Label")).toBeInTheDocument();

    // Icon should be rendered (since we passed Lucide component)
    expect(document.querySelector("svg")).toBeInTheDocument();
  });

  it("should format large numbers using toLocaleString", () => {
    // 1000 => "1,000" or depends on locale, but checking it's formatted
    render(<ProfileStatsCard icon={User} value={1000} label="Points" />);

    const valueEl = screen.getByText(/1[,.]000/);
    expect(valueEl).toBeInTheDocument();
  });

  it("should call onClick when clicked", () => {
    const mockOnClick = vi.fn();
    render(
      <ProfileStatsCard
        icon={User}
        value="Text"
        label="Label"
        onClick={mockOnClick}
      />,
    );

    const cards = screen.getAllByTestId("motion-div");
    fireEvent.click(cards[0]);

    expect(mockOnClick).toHaveBeenCalledTimes(1);
  });
});
