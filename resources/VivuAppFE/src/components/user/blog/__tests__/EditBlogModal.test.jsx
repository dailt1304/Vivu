import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import EditBlogModal from "../EditBlogModal";

const mockBlog = {
  id: "blog-123",
  title: "Test Blog Title",
  shortDescription: "Test description",
  coverImageUrl: "https://example.com/cover.jpg",
};

describe("EditBlogModal", () => {
  it("renders nothing when isOpen is false", () => {
    const { container } = render(
      <EditBlogModal isOpen={false} blog={mockBlog} onClose={vi.fn()} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders nothing when blog is null", () => {
    const { container } = render(
      <EditBlogModal isOpen={true} blog={null} onClose={vi.fn()} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders form fields when open with blog data", () => {
    render(
      <EditBlogModal
        isOpen={true}
        blog={mockBlog}
        onClose={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    expect(screen.getByText("Chỉnh sửa bài viết")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Test Blog Title")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Test description")).toBeInTheDocument();
  });

  it("calls onClose when cancel button is clicked", () => {
    const onClose = vi.fn();
    render(
      <EditBlogModal
        isOpen={true}
        blog={mockBlog}
        onClose={onClose}
        onSubmit={vi.fn()}
      />,
    );
    fireEvent.click(screen.getByText("Hủy"));
    expect(onClose).toHaveBeenCalled();
  });

  it("calls onSubmit with correct data when form is submitted", () => {
    const onSubmit = vi.fn();
    render(
      <EditBlogModal
        isOpen={true}
        blog={mockBlog}
        onClose={vi.fn()}
        onSubmit={onSubmit}
      />,
    );

    // Change title
    const titleInput = screen.getByDisplayValue("Test Blog Title");
    fireEvent.change(titleInput, { target: { value: "Updated Title" } });

    // Submit form
    fireEvent.click(screen.getByText("Lưu thay đổi"));

    expect(onSubmit).toHaveBeenCalledTimes(1);
    const callArg = onSubmit.mock.calls[0][0];
    expect(callArg.blogId).toBe("blog-123");
    expect(callArg.formData).toBeInstanceOf(FormData);
    expect(callArg.formData.get("Title")).toBe("Updated Title");
  });

  it("disables submit button when title is empty", () => {
    render(
      <EditBlogModal
        isOpen={true}
        blog={{ ...mockBlog, title: "" }}
        onClose={vi.fn()}
        onSubmit={vi.fn()}
      />,
    );
    const submitBtn = screen.getByText("Lưu thay đổi").closest("button");
    expect(submitBtn).toBeDisabled();
  });

  it("shows loading state when isSubmitting is true", () => {
    render(
      <EditBlogModal
        isOpen={true}
        blog={mockBlog}
        onClose={vi.fn()}
        onSubmit={vi.fn()}
        isSubmitting={true}
      />,
    );
    expect(screen.getByText("Đang lưu...")).toBeInTheDocument();
  });
});
