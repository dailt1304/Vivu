import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import Footer from "../Footer";

const renderWithRouter = (ui) => {
  return render(<BrowserRouter>{ui}</BrowserRouter>);
};

describe("Footer", () => {
  it("should render application branding and description", () => {
    renderWithRouter(<Footer />);
    // Check main brand
    const brands = screen.getAllByText(/Vivu/i);
    expect(brands[0]).toBeInTheDocument();
    // Check description text
    expect(
      screen.getByText(/Người bạn đồng hành tin cậy cho những chuyến đi/i),
    ).toBeInTheDocument();
  });

  it("should render navigation headers", () => {
    renderWithRouter(<Footer />);
    expect(screen.getByText(/Về Vivu/i)).toBeInTheDocument();
    expect(screen.getByText(/Hỗ trợ/i)).toBeInTheDocument();
    expect(screen.getByText(/Điểm đến/i)).toBeInTheDocument();
  });

  it("should render copyright and footer links", () => {
    renderWithRouter(<Footer />);
    expect(screen.getByText(/2026 Vivu/i)).toBeInTheDocument();
    expect(screen.getByText(/Chính sách bảo mật/i)).toBeInTheDocument();
    expect(screen.getByText(/Điều khoản sử dụng/i)).toBeInTheDocument();
  });
});
