import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { useRef } from "react";
import useClickOutside from "../useClickOutside";

// A dummy component to test the hook in a real DOM scenario
const TestComponent = ({ onOutsideClick }) => {
  const ref = useRef(null);
  useClickOutside(ref, onOutsideClick);

  return (
    <div>
      <div data-testid="outside">Outside Element</div>
      <div ref={ref} data-testid="inside">
        <button data-testid="inside-btn">Inside Button</button>
      </div>
    </div>
  );
};

describe("useClickOutside", () => {
  it("should call handler when clicking outside the ref", () => {
    const handler = vi.fn();
    render(<TestComponent onOutsideClick={handler} />);

    // Click on the outside element
    const outsideEl = screen.getByTestId("outside");
    fireEvent.mouseDown(outsideEl);

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("should NOT call handler when clicking inside the ref", () => {
    const handler = vi.fn();
    render(<TestComponent onOutsideClick={handler} />);

    // Click on the inner element itself
    const insideEl = screen.getByTestId("inside");
    fireEvent.mouseDown(insideEl);

    // Click on a nested element inside the ref
    const insideBtn = screen.getByTestId("inside-btn");
    fireEvent.mouseDown(insideBtn);

    expect(handler).not.toHaveBeenCalled();
  });

  it("should work with touchstart events", () => {
    const handler = vi.fn();
    render(<TestComponent onOutsideClick={handler} />);

    // Touch on the outside element
    const outsideEl = screen.getByTestId("outside");
    fireEvent.touchStart(outsideEl);

    expect(handler).toHaveBeenCalledTimes(1);
  });
});
