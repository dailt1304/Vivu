import { describe, it, expect, beforeEach, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import useAuth from "../useAuth";

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const wrapper = ({ children }) => <MemoryRouter>{children}</MemoryRouter>;

describe("useAuth", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("should return isAuthenticated=false when localStorage is empty", () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.userId).toBeNull();
  });

  it("should return isAuthenticated=true when access_token exists", () => {
    localStorage.setItem("access_token", "valid-token");
    localStorage.setItem(
      "user",
      JSON.stringify({ id: "u1", fullName: "Test" }),
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual({ id: "u1", fullName: "Test" });
  });

  it("should save tokens and user data on login()", () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    const userData = { id: "u1", fullName: "John" };

    act(() => {
      result.current.login("access-tk", "refresh-tk", userData);
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(localStorage.getItem("access_token")).toBe("access-tk");
    expect(localStorage.getItem("refresh_token")).toBe("refresh-tk");
    expect(JSON.parse(localStorage.getItem("user"))).toEqual(userData);
  });

  it("should clear all auth data and navigate on logout()", () => {
    localStorage.setItem("access_token", "tk");
    localStorage.setItem("refresh_token", "rf");
    localStorage.setItem("user", JSON.stringify({ id: "u1" }));

    const { result } = renderHook(() => useAuth(), { wrapper });

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem("access_token")).toBeNull();
    expect(localStorage.getItem("refresh_token")).toBeNull();
    expect(localStorage.getItem("user")).toBeNull();
    expect(mockNavigate).toHaveBeenCalledWith("/login");
  });

  it("should parse userId from nested data structure", () => {
    localStorage.setItem("access_token", "tk");
    localStorage.setItem(
      "user",
      JSON.stringify({ data: { id: "nested-id", fullName: "Nested" } }),
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.userId).toBe("nested-id");
    expect(result.current.user).toEqual({
      id: "nested-id",
      fullName: "Nested",
    });
  });

  it("should update user data on refreshUser()", () => {
    localStorage.setItem("access_token", "tk");
    localStorage.setItem("user", JSON.stringify({ id: "u1", fullName: "Old" }));

    const { result } = renderHook(() => useAuth(), { wrapper });

    const newUserData = { id: "u1", fullName: "Updated Name" };

    act(() => {
      result.current.refreshUser(newUserData);
    });

    expect(result.current.user).toEqual(newUserData);
    expect(JSON.parse(localStorage.getItem("user"))).toEqual(newUserData);
  });
});
