import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import useTypewriterBuffer from "../useTypewriterBuffer";

describe("useTypewriterBuffer", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("initializes with empty text", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));
    expect(result.current.displayedText).toBe("");
  });

  it("appends text character by character over time using target text", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));

    act(() => {
      // Set the absolute target text
      result.current.setTargetText("Hello");
    });

    // Initially nothing should be rendered since setInterval hasn't fired yet
    expect(result.current.displayedText).toBe("");

    // Advance time to allow characters to be extracted from buffer
    act(() => {
      vi.advanceTimersByTime(16);
    });
    expect(result.current.displayedText).toBe("H");

    act(() => {
      vi.advanceTimersByTime(16);
    });
    expect(result.current.displayedText).toBe("He");

    act(() => {
      vi.advanceTimersByTime(100);
    });
    expect(result.current.displayedText).toBe("Hello");
  });

  it("handles changing target text mid-animation gracefully", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));

    act(() => {
      result.current.setTargetText("Hi");
    });

    act(() => {
      vi.advanceTimersByTime(32); // Should render 'Hi'
    });
    expect(result.current.displayedText).toBe("Hi");

    // Push new absolute target text while animating
    act(() => {
      result.current.setTargetText("Hi there!");
    });

    // We shouldn't jump, we should resume typing from 'Hi' towards 'Hi there!'
    act(() => {
      vi.advanceTimersByTime(16);
    });
    expect(result.current.displayedText).toBe("Hi ");

    act(() => {
      vi.advanceTimersByTime(150);
    });
    expect(result.current.displayedText).toBe("Hi there!");
  });

  it("handles trimming target text abruptly", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));

    act(() => {
      result.current.setTargetText("Long text testing");
      vi.advanceTimersByTime(100);
    });
    expect(result.current.displayedText.length).toBeGreaterThan(3);

    act(() => {
      // Shrink target text drastically below the current cursor
      result.current.setTargetText("Lo");
    });

    // Expected to snap shrink to fit boundaries
    expect(result.current.displayedText).toBe("Lo");
  });

  it("flushes remaining buffer instantly to absolute target", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));

    act(() => {
      result.current.setTargetText(
        "This is a very long text that we want to flush.",
      );
    });

    // Advance by some frames to show it's working
    act(() => {
      vi.advanceTimersByTime(32);
    });

    expect(result.current.displayedText).toBe("Th");

    act(() => {
      result.current.flush();
      vi.advanceTimersByTime(0); // Let any pending microtasks flush
    });

    expect(result.current.displayedText).toBe(
      "This is a very long text that we want to flush.",
    );
  });

  it("resets completely", () => {
    const { result } = renderHook(() => useTypewriterBuffer(16, 1));

    act(() => {
      result.current.setTargetText("Testing reset");
      vi.advanceTimersByTime(48);
    });
    expect(result.current.displayedText).toBe("Tes");

    act(() => {
      result.current.reset();
    });

    expect(result.current.displayedText).toBe("");

    // Ensure it's dead and no more timers fire text
    act(() => {
      vi.advanceTimersByTime(100);
    });
    expect(result.current.displayedText).toBe("");
  });
});
