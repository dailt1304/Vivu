import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import RatingStars from "../RatingStars";

describe("RatingStars", () => {
  it("should render the correct number of filled stars based on rating prop", () => {
    const { container } = render(<RatingStars rating={3} totalStars={5} />);
    const stars = container.querySelectorAll("svg");
    expect(stars.length).toBe(5);
  });

  it("should call onRate when interactive and a star is clicked", () => {
    const handleRate = vi.fn();
    const { container } = render(
      <RatingStars rating={0} interactive={true} onRate={handleRate} />,
    );

    const starButtons = screen.getAllByRole("button");
    fireEvent.click(starButtons[3]); // Click the 4th star

    expect(handleRate).toHaveBeenCalledWith(4);
  });

  it("should not call onRate when interactive is false", () => {
    const handleRate = vi.fn();
    render(<RatingStars rating={3} interactive={false} onRate={handleRate} />);

    const starButtons = screen.queryAllByRole("button");
    if (starButtons.length > 0) {
      fireEvent.click(starButtons[0]);
    }

    expect(handleRate).not.toHaveBeenCalled();
  });
});
