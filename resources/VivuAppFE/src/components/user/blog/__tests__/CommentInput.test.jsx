import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import CommentInput from "../CommentInput";

describe("CommentInput", () => {
  it("should render textarea and submit button", () => {
    const { container } = render(<CommentInput onSubmit={vi.fn()} />);
    expect(screen.getByPlaceholderText(/Viết bình luận/i)).toBeInTheDocument();
    expect(
      container.querySelector('button[type="submit"]'),
    ).toBeInTheDocument();
  });

  it("should call onSubmit with input text when submitted", () => {
    const handleSubmit = vi.fn();
    const { container } = render(<CommentInput onSubmit={handleSubmit} />);

    const input = screen.getByPlaceholderText(/Viết bình luận/i);
    const button = container.querySelector('button[type="submit"]');

    fireEvent.change(input, { target: { value: "Bài viết hay quá!" } });
    fireEvent.click(button);

    expect(handleSubmit).toHaveBeenCalledWith("Bài viết hay quá!");
    expect(input.value).toBe("");
  });

  it("should disable submit button when input text is empty", () => {
    const { container } = render(<CommentInput onSubmit={vi.fn()} />);

    const button = container.querySelector('button[type="submit"]');
    expect(button).toBeDisabled();
  });
});
