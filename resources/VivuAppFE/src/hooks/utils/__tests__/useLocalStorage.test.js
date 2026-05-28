import { describe, it, expect, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import useLocalStorage from "../useLocalStorage";

describe("useLocalStorage", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("should return initialValue when key does not exist", () => {
    const { result } = renderHook(() =>
      useLocalStorage("missing-key", "default"),
    );
    expect(result.current[0]).toBe("default");
  });

  it("should read existing value from localStorage on mount", () => {
    localStorage.setItem("existing-key", JSON.stringify({ name: "test" }));

    const { result } = renderHook(() => useLocalStorage("existing-key", null));
    expect(result.current[0]).toEqual({ name: "test" });
  });

  it("should write value to localStorage via setValue", () => {
    const { result } = renderHook(() => useLocalStorage("write-key", ""));

    act(() => {
      result.current[1]("new-value");
    });

    expect(result.current[0]).toBe("new-value");
    expect(JSON.parse(localStorage.getItem("write-key"))).toBe("new-value");
  });

  it("should support callback-style setValue", () => {
    const { result } = renderHook(() => useLocalStorage("counter", 0));

    act(() => {
      result.current[1]((prev) => prev + 1);
    });

    expect(result.current[0]).toBe(1);

    act(() => {
      result.current[1]((prev) => prev + 10);
    });

    expect(result.current[0]).toBe(11);
  });

  it("should remove value and reset to initialValue via removeValue", () => {
    localStorage.setItem("remove-key", JSON.stringify("existing"));

    const { result } = renderHook(() =>
      useLocalStorage("remove-key", "default"),
    );

    expect(result.current[0]).toBe("existing");

    act(() => {
      result.current[2](); // removeValue
    });

    expect(result.current[0]).toBe("default");
    expect(localStorage.getItem("remove-key")).toBeNull();
  });

  it("should return initialValue when stored JSON is corrupted", () => {
    // Directly set invalid JSON (bypassing the hook)
    localStorage.setItem("corrupt-key", "{{not-valid-json");

    const { result } = renderHook(() =>
      useLocalStorage("corrupt-key", "fallback"),
    );
    expect(result.current[0]).toBe("fallback");
  });
});
