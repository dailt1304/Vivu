import { renderHook, act } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import useGeolocation from "../useGeolocation";

describe("useGeolocation", () => {
  beforeEach(() => {
    // Reset globalThis navigator
    globalThis.navigator.geolocation = {
      getCurrentPosition: vi.fn(),
    };
  });

  it("should return null position initially", () => {
    const { result } = renderHook(() => useGeolocation());
    expect(result.current.position).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
  });

  it("should set position after successful geolocation", () => {
    const mockPosition = {
      coords: {
        latitude: 16.047,
        longitude: 108.206,
        accuracy: 10,
      },
    };

    globalThis.navigator.geolocation.getCurrentPosition.mockImplementationOnce(
      (success) => success(mockPosition),
    );

    const { result } = renderHook(() => useGeolocation());

    act(() => {
      result.current.requestPosition();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.position).toEqual({
      latitude: 16.047,
      longitude: 108.206,
      accuracy: 10,
    });
  });

  it("should set Vietnamese error on permission denied (code 1)", () => {
    const mockError = { code: 1, message: "User denied Geolocation" };

    globalThis.navigator.geolocation.getCurrentPosition.mockImplementationOnce(
      (_, error) => error(mockError),
    );

    const { result } = renderHook(() => useGeolocation());

    act(() => {
      result.current.requestPosition();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe("Bạn đã từ chối quyền truy cập vị trí");
    expect(result.current.position).toBeNull();
  });

  it("should set error on timeout (code 3)", () => {
    const mockError = { code: 3, message: "Timeout" };

    globalThis.navigator.geolocation.getCurrentPosition.mockImplementationOnce(
      (_, error) => error(mockError),
    );

    const { result } = renderHook(() => useGeolocation());

    act(() => {
      result.current.requestPosition();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe("Hết thời gian chờ định vị");
  });

  it("should set unhandled error messages directly", () => {
    const mockError = { code: 99, message: "Some unknown error" };

    globalThis.navigator.geolocation.getCurrentPosition.mockImplementationOnce(
      (_, error) => error(mockError),
    );

    const { result } = renderHook(() => useGeolocation());

    act(() => {
      result.current.requestPosition();
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBe("Some unknown error");
  });

  it("should handle missing geolocation API", () => {
    delete globalThis.navigator.geolocation;

    const { result } = renderHook(() => useGeolocation());

    act(() => {
      result.current.requestPosition();
    });

    expect(result.current.error).toBe("Trình duyệt không hỗ trợ định vị");
  });
});
