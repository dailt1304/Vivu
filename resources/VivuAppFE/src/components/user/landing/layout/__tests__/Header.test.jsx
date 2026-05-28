import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import Header from "../Header";

const renderWithRouter = (ui) => {
  return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe("Header", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should render logo and primary navigation links", () => {
    renderWithRouter(<Header />);

    // Check Logo
    expect(screen.getByText(/Vivu/i)).toBeInTheDocument();

    // Check Links
    expect(screen.getByText(/Điểm đến/i)).toBeInTheDocument();
    expect(screen.getByText(/Cách thức hoạt động/i)).toBeInTheDocument();
    expect(screen.getByText(/Trải nghiệm/i)).toBeInTheDocument();
    expect(screen.getByText(/Câu chuyện/i)).toBeInTheDocument();
  });

  it("should render 'Đăng nhập' and 'Bắt đầu' buttons", () => {
    renderWithRouter(<Header />);

    expect(screen.getByText(/Đăng nhập/i)).toBeInTheDocument();
    expect(screen.getByText(/Bắt đầu/i)).toBeInTheDocument();
  });
});
