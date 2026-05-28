import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router-dom";
import BlogCard from "../BlogCard";

// Mock useNavigate
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

const mockBlogData = {
  id: "blog1",
  title: "Kinh nghiệm du lịch Đà Nẵng",
  author: "John Doe",
  image: "cover.jpg",
  excerpt: "Đà Nẵng có rất nhiều chỗ chơi...",
  date: "15 THÁNG 3, 2024",
};

describe("BlogCard", () => {
  it("should render blog title, excerpt, and author name", () => {
    render(
      <MemoryRouter>
        <BlogCard data={mockBlogData} />
      </MemoryRouter>,
    );

    expect(screen.getByText("Kinh nghiệm du lịch Đà Nẵng")).toBeInTheDocument();
    expect(
      screen.getByText(/Đà Nẵng có rất nhiều chỗ chơi/i),
    ).toBeInTheDocument();
    expect(screen.getByText("John Doe")).toBeInTheDocument();
    expect(screen.getByText("15 THÁNG 3, 2024")).toBeInTheDocument();
  });

  it("should navigate to blog detail when clicked", () => {
    const mockNavigate = vi.fn();
    vi.mocked(useNavigate).mockReturnValue(mockNavigate);

    render(
      <MemoryRouter>
        <BlogCard data={mockBlogData} />
      </MemoryRouter>,
    );

    const card = screen
      .getByText("Kinh nghiệm du lịch Đà Nẵng")
      .closest('div[class*="group"]');
    fireEvent.click(card);

    expect(mockNavigate).toHaveBeenCalledWith("/inspiration/blog1");
  });
});
