import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import useDebounce from "../useDebounce";

describe("useDebounce", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.clearAllTimers();
    vi.useRealTimers();
  });

  it("should return the initial value immediately", () => {
    const { result } = renderHook(() => useDebounce("initial", 500));
    expect(result.current).toBe("initial");
  });

  it("should delay updating the value until the timeout has passed", () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: "initial", delay: 500 } },
    );

    // Update the value
    rerender({ value: "updated", delay: 500 });

    // Instantly after rerender, it should still be "initial"
    expect(result.current).toBe("initial");

    // Fast-forward 499ms, should still be "initial"
    act(() => {
      vi.advanceTimersByTime(499);
    });
    expect(result.current).toBe("initial");

    // Fast-forward the last 1ms
    act(() => {
      vi.advanceTimersByTime(1);
    });

    // NOW it should be "updated"
    expect(result.current).toBe("updated");
  });

  it("should reset the timeout if the value changes before the delay completes", () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: "A" } },
    );

    expect(result.current).toBe("A");

    // Change to "B"
    rerender({ value: "B" });
    act(() => {
      vi.advanceTimersByTime(200);
    });
    // Should still be "A" because only 200ms passed
    expect(result.current).toBe("A");

    // Change to "C" before "B" resolves
    rerender({ value: "C" });
    act(() => {
      vi.advanceTimersByTime(200);
    });
    // Should STILL be "A", because the 300ms timer restarted for "C"
    expect(result.current).toBe("A");

    // Advance 100 more ms to finish "C"s 300ms timer
    act(() => {
      vi.advanceTimersByTime(100);
    });

    // Now it should finally update to "C"
    expect(result.current).toBe("C");
  });
});
