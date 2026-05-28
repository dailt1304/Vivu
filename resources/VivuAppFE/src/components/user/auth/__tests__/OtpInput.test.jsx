import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import OtpInput from "../OtpInput";

describe("OtpInput", () => {
  it("should render default 6 input fields", () => {
    render(<OtpInput onComplete={vi.fn()} />);
    const inputs = screen.getAllByRole("textbox");
    expect(inputs).toHaveLength(6);
  });

  it("should focus the next input when a digit is entered", async () => {
    render(<OtpInput onComplete={vi.fn()} autocomplete={false} />);
    const inputs = screen.getAllByRole("textbox");

    // Type "5" in the first input
    fireEvent.change(inputs[0], { target: { value: "5" } });

    // First input should hold value "5"
    expect(inputs[0].value).toBe("5");

    // The second input should now have focus
    await waitFor(() => {
      expect(inputs[1]).toHaveFocus();
    });
  });

  it("should trigger onComplete when all fields are filled", () => {
    const mockOnComplete = vi.fn();
    render(<OtpInput onComplete={mockOnComplete} />);
    const inputs = screen.getAllByRole("textbox");

    const code = "123456";
    // Simulate user typing the code digit by digit
    code.split("").forEach((digit, index) => {
      fireEvent.change(inputs[index], { target: { value: digit } });
    });

    expect(mockOnComplete).toHaveBeenCalledWith(code);
    expect(mockOnComplete).toHaveBeenCalledTimes(1);
  });

  it("should focus the previous input when Backspace is pressed on an empty input", () => {
    // Need a focusable environment
    const { container } = render(<OtpInput onComplete={vi.fn()} />);
    const inputs = screen.getAllByRole("textbox");

    // Manually set focus to input 2 (index 1)
    inputs[1].focus();

    // Press backspace
    fireEvent.keyDown(inputs[1], { key: "Backspace" });

    // The first input (index 0) should now have focus
    expect(inputs[0]).toHaveFocus();
  });
});
