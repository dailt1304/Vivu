import { describe, it, expect, beforeEach, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { AuthProvider } from "../AuthContext";
import { useAuth } from "../auth-context";
import { MemoryRouter } from "react-router-dom";

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("AuthContext", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  const wrapper = ({ children }) => (
    <MemoryRouter>
      <AuthProvider>{children}</AuthProvider>
    </MemoryRouter>
  );

  it("should initialize as unauthenticated when localStorage is empty", () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.userId).toBeNull();
  });

  it("should initialize as authenticated when tokens exist in localStorage", () => {
    const mockUser = { id: "123", email: "test@example.com" };
    localStorage.setItem("access_token", "fake-token");
    localStorage.setItem("user", JSON.stringify(mockUser));

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual({ ...mockUser, roles: [] });
    expect(result.current.userId).toBe("123");
  });

  it("should handle login correctly", () => {
    const { result } = renderHook(() => useAuth(), { wrapper });
    const mockUser = { id: "456", email: "login@example.com" };

    act(() => {
      result.current.login("new-access-token", "new-refresh-token", mockUser);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual({ ...mockUser, roles: [] });
    expect(localStorage.getItem("access_token")).toBe("new-access-token");
    expect(localStorage.getItem("refresh_token")).toBe("new-refresh-token");
    expect(JSON.parse(localStorage.getItem("user"))).toEqual(mockUser);
  });

  it("should handle logout correctly", () => {
    // Setup initial state
    localStorage.setItem("access_token", "fake-token");
    localStorage.setItem("refresh_token", "fake-refresh");
    localStorage.setItem("user", JSON.stringify({ id: "123" }));

    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(localStorage.getItem("access_token")).toBeNull();
    expect(localStorage.getItem("refresh_token")).toBeNull();
    expect(localStorage.getItem("user")).toBeNull();
    expect(mockNavigate).toHaveBeenCalledWith("/login");
  });

  it("should refresh user data correctly", () => {
    localStorage.setItem("access_token", "fake-token");
    localStorage.setItem(
      "user",
      JSON.stringify({ id: "123", name: "Old Name" }),
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    const updatedUser = { id: "123", name: "New Name" };

    act(() => {
      result.current.refreshUser(updatedUser);
    });

    expect(result.current.user).toEqual({ ...updatedUser, roles: [] });
    expect(JSON.parse(localStorage.getItem("user"))).toEqual(updatedUser);
  });
});
