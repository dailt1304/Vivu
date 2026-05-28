import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router-dom";
import RegisterForm from "../RegisterForm";
import authApi from "../../../../api/authApi";

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

describe("RegisterForm", () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useNavigate.mockReturnValue(mockNavigate);
  });

  const renderWithRouter = () => {
    return render(
      <MemoryRouter>
        <RegisterForm />
      </MemoryRouter>,
    );
  };

  it("should render registration form with name, email, password fields", () => {
    renderWithRouter();

    expect(screen.getByPlaceholderText(/Nguyễn Văn A/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/you@example.com/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/••••••••/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /Tiếp tục|Tạo tài khoản/i }),
    ).toBeInTheDocument();
  });

  it("should validate required fields", async () => {
    // Test that the form does not send OTP if fields are invalid
    renderWithRouter();

    const nameInput = screen.getByPlaceholderText(/Nguyễn Văn A/i);
    const emailInput = screen.getByPlaceholderText(/you@example.com/i);
    const passwordInput = screen.getByPlaceholderText(/••••••••/i);

    fireEvent.change(nameInput, { target: { value: "" } });
    fireEvent.blur(nameInput);
    fireEvent.change(emailInput, { target: { value: "invalid-email" } });
    fireEvent.blur(emailInput);
    fireEvent.change(passwordInput, { target: { value: "pass" } });
    fireEvent.blur(passwordInput);

    const submitButton = screen.getByRole("button", { name: /Tiếp tục/i });
    fireEvent.submit(submitButton.closest("form"));

    await waitFor(() => {
      expect(screen.getByText("Họ và tên là bắt buộc")).toBeInTheDocument();
      expect(screen.getByText("Email không hợp lệ")).toBeInTheDocument();
      expect(authApi.sendVerificationEmail).not.toHaveBeenCalled();
    });
  });

  it("should show error when email format is invalid", async () => {
    renderWithRouter();

    const nameInput = screen.getByPlaceholderText(/Nguyễn Văn A/i);
    const emailInput = screen.getByPlaceholderText(/you@example.com/i);

    fireEvent.change(nameInput, { target: { value: "Test User" } });
    fireEvent.change(emailInput, { target: { value: "invalid-email" } });

    // Trigger blur validation
    fireEvent.blur(emailInput);

    await waitFor(() => {
      expect(screen.getByText("Email không hợp lệ")).toBeInTheDocument();
    });
  });

  it("should send verification email and transition to OTP step on valid submission", async () => {
    authApi.sendVerificationEmail.mockResolvedValueOnce({
      success: true,
      message: "Success",
    });

    renderWithRouter();

    const nameInput = screen.getByPlaceholderText(/Nguyễn Văn A/i);
    const emailInput = screen.getByPlaceholderText(/you@example.com/i);
    const passwordInput = screen.getByPlaceholderText(/••••••••/i);

    fireEvent.change(nameInput, { target: { value: "Test User" } });
    fireEvent.change(emailInput, { target: { value: "test@example.com" } });
    fireEvent.change(passwordInput, { target: { value: "Password123" } });

    const submitButton = screen.getByRole("button", { name: /Tiếp tục/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(authApi.sendVerificationEmail).toHaveBeenCalledWith({
        email: "test@example.com",
      });
      // After success, it should show OTP UI (step 2)
      expect(screen.getByText(/Xác thực Email/i)).toBeInTheDocument();
      expect(screen.getByText(/Mã xác thực 6 số/i)).toBeInTheDocument();
    });
  });
});
