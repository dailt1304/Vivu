import { describe, it, expect, beforeEach, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import ProtectedRoute from "../ProtectedRoute";

// Mock BottomNavbar to avoid rendering complex navigation
vi.mock("../../../layout/BottomNavbar", () => ({
  default: () => <div data-testid="bottom-navbar">Navbar</div>,
}));

const renderWithRoute = (initialPath = "/protected") => {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/protected" element={<div>Secret Page</div>} />
        </Route>
        <Route path="/login" element={<div>Login Page</div>} />
      </Routes>
    </MemoryRouter>,
  );
};

describe("ProtectedRoute", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("should redirect to /login when no access_token exists", () => {
    renderWithRoute();

    expect(screen.getByText("Login Page")).toBeInTheDocument();
    expect(screen.queryByText("Secret Page")).not.toBeInTheDocument();
  });

  it("should render child route and BottomNavbar when access_token exists", () => {
    localStorage.setItem("access_token", "valid-token");

    renderWithRoute();

    expect(screen.getByText("Secret Page")).toBeInTheDocument();
    expect(screen.getByTestId("bottom-navbar")).toBeInTheDocument();
    expect(screen.queryByText("Login Page")).not.toBeInTheDocument();
  });

  it("should redirect when access_token is empty string", () => {
    localStorage.setItem("access_token", "");

    renderWithRoute();

    // Empty string is falsy, so should redirect
    expect(screen.getByText("Login Page")).toBeInTheDocument();
  });
});
