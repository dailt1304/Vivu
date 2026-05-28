import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import CMSProtectedRoute from "../CMSProtectedRoute";
import { AuthContext } from "../../../contexts/auth-context";

vi.mock("../layout/CMSLayout", () => ({
  default: () => <div data-testid="cms-layout">CMS Layout Mock</div>,
}));

describe("CMSProtectedRoute", () => {
  const renderWithAuth = (authValue) => {
    return render(
      <AuthContext.Provider value={authValue}>
        <MemoryRouter initialEntries={["/cms"]}>
          <Routes>
            <Route path="/" element={<div data-testid="home">Home</div>} />
            <Route
              path="/login"
              element={<div data-testid="login">Login</div>}
            />
            <Route path="/cms" element={<CMSProtectedRoute />} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    );
  };

  it("should render loading spinner if isLoading is true", () => {
    const { container } = renderWithAuth({
      isLoading: true,
      isAuthenticated: false,
      user: null,
    });
    // Check if the spinner element is in the document (the div with border-primary class)
    expect(container.querySelector(".border-primary")).toBeInTheDocument();
  });

  it("should redirect to login if user is not authenticated", () => {
    renderWithAuth({ isLoading: false, isAuthenticated: false, user: null });
    // It should navigate away from CMS, so cms-layout is not rendered
    expect(screen.queryByTestId("cms-layout")).not.toBeInTheDocument();
  });

  it("should return 403 if user is authenticated but not ADMIN", () => {
    renderWithAuth({ isAuthenticated: true, user: { roles: ["USER"] } });
    expect(screen.getByText("403")).toBeInTheDocument();
  });

  it("should render CMSLayout if user is authenticated and is ADMIN", () => {
    renderWithAuth({ isAuthenticated: true, user: { roles: ["ADMIN"] } });
    expect(screen.getByTestId("cms-layout")).toBeInTheDocument();
  });
});
