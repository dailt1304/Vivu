import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router-dom";
import LoginForm from "../LoginForm";
import authApi from "../../../../api/authApi";
import { AuthContext } from "../../../../contexts/auth-context";
import toast from "../../../../utils/toast";

// Mock external dependencies
vi.mock("../../../../api/authApi");
vi.mock("../../../../utils/toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));
vi.mock("@react-oauth/google", () => ({
  GoogleLogin: () => <button data-testid="google-login">Google Login</button>,
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: vi.fn(),
  };
});

// Provide a mock auth context
const renderWithProviders = (ui, { providerProps } = {}) => {
  return render(
    <MemoryRouter>
      <AuthContext.Provider value={providerProps}>{ui}</AuthContext.Provider>
    </MemoryRouter>,
  );
};

describe("LoginForm", () => {
  const mockNavigate = vi.fn();
  const mockLogin = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useNavigate.mockReturnValue(mockNavigate);
  });

  const defaultProps = {
    login: mockLogin,
    isAuthenticated: false,
  };

  it("should render login form with email, password fields and submission button", () => {
    renderWithProviders(<LoginForm />, { providerProps: defaultProps });

    expect(screen.getByPlaceholderText(/you@example.com/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/••••••••/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /^Đăng nhập$/i }),
    ).toBeInTheDocument();
  });

  it("should have required attributes on email and password fields", () => {
    // Only filling email, leaving password blank
    renderWithProviders(<LoginForm />, { providerProps: defaultProps });

    const emailInput = screen.getByPlaceholderText(/you@example.com/i);
    const passwordInput = screen.getByPlaceholderText(/••••••••/i);

    expect(emailInput).toBeRequired();
    expect(passwordInput).toBeRequired();
  });

  it("should call authApi.login, navigate on successful submission", async () => {
    authApi.login.mockResolvedValueOnce({
      success: true,
      data: {
        accessToken: "valid-token",
        refreshToken: "refresh-token",
      },
      message: "Success",
    });

    authApi.getMe.mockResolvedValueOnce({
      data: { id: "user-123", roles: ["USER"] },
    });

    renderWithProviders(<LoginForm />, { providerProps: defaultProps });

    const emailInput = screen.getByPlaceholderText(/you@example.com/i);
    const passwordInput = screen.getByPlaceholderText(/••••••••/i);

    fireEvent.change(emailInput, { target: { value: "test@example.com" } });
    fireEvent.change(passwordInput, { target: { value: "password123" } });

    const submitButton = screen.getByRole("button", { name: /^Đăng nhập$/i });
    fireEvent.submit(submitButton.closest("form"));

    await waitFor(() => {
      // Validate authApi called
      expect(authApi.login).toHaveBeenCalledWith({
        email: "test@example.com",
        password: "password123",
      });
      // Validate context login removed since component uses localStorage directly
      // Validate navigation (normal user goes to /chat)
      expect(mockNavigate).toHaveBeenCalledWith("/chat");
    });
  });

  it("should show error toast on failed login", async () => {
    // Mock failing API
    authApi.login.mockRejectedValueOnce(new Error("Invalid credentials"));

    renderWithProviders(<LoginForm />, { providerProps: defaultProps });

    const emailInput = screen.getByPlaceholderText(/you@example.com/i);
    const passwordInput = screen.getByPlaceholderText(/••••••••/i);

    fireEvent.change(emailInput, { target: { value: "test@example.com" } });
    fireEvent.change(passwordInput, { target: { value: "wrongpassword" } });

    const submitButton = screen.getByRole("button", { name: /^Đăng nhập$/i });
    fireEvent.submit(submitButton.closest("form"));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith(
        "Đăng nhập thất bại. Vui lòng thử lại.",
      );
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });
});
