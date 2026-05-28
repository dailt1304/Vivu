import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useGridColumns } from "../useGridColumns";

describe("useGridColumns", () => {
  const originalInnerWidth = window.innerWidth;

  const setWindowWidth = (width) => {
    window.innerWidth = width;
    window.dispatchEvent(new Event("resize"));
  };

  afterEach(() => {
    // Restore window object after tests
    window.innerWidth = originalInnerWidth;
  });

  it("should return 1 column for mobile screens (< 640px)", () => {
    window.innerWidth = 375;
    const { result } = renderHook(() => useGridColumns());

    expect(result.current).toBe(1);
  });

  it("should return 2 columns for tablet screens (>= 640px)", () => {
    window.innerWidth = 768;
    const { result } = renderHook(() => useGridColumns());

    expect(result.current).toBe(2);
  });

  it("should return 3 columns for desktop screens (>= 1024px)", () => {
    window.innerWidth = 1024;
    const { result } = renderHook(() => useGridColumns());

    expect(result.current).toBe(3);
  });

  it("should return 4 columns for large desktop screens (>= 1280px)", () => {
    window.innerWidth = 1440;
    const { result } = renderHook(() => useGridColumns());

    expect(result.current).toBe(4);
  });

  it("should dynamically update columns on window resize", () => {
    window.innerWidth = 375; // Initial mobile
    const { result } = renderHook(() => useGridColumns());

    expect(result.current).toBe(1);

    // Resize to large desktop
    act(() => {
      setWindowWidth(1440);
    });

    expect(result.current).toBe(4);

    // Resize back to tablet
    act(() => {
      setWindowWidth(800);
    });

    expect(result.current).toBe(2);
  });
});
