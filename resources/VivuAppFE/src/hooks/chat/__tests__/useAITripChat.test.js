import { renderHook, act } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { useAITripChat } from "../useAITripChat";
import aiApi from "../../../api/aiApi";

vi.mock("../../../api/aiApi", () => ({
  default: {
    streamTripChat: vi.fn(),
  },
}));

describe("useAITripChat", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should initialize with default states", () => {
    const { result } = renderHook(() => useAITripChat("trip-123"));
    expect(result.current.isStreaming).toBe(false);
    expect(result.current.streamedText).toBe("");
  });

  it("should start streaming, update text on chunk, and stop on complete", async () => {
    const tripId = "trip-123";
    const message = "test message";
    const connectionId = "conn-1";

    aiApi.streamTripChat.mockImplementation(
      async (tId, msg, connId, callbacks) => {
        callbacks.onStart?.({});
        callbacks.onChunk?.("Hello ");
        callbacks.onChunk?.("World!");
        callbacks.onComplete?.({});
      },
    );

    const { result } = renderHook(() => useAITripChat(tripId));

    const onStart = vi.fn();
    const onChunk = vi.fn();
    const onComplete = vi.fn();

    await act(async () => {
      await result.current.sendAIMessage(message, connectionId, {
        onStart,
        onChunk,
        onComplete,
      });
    });

    expect(aiApi.streamTripChat).toHaveBeenCalledWith(
      tripId,
      message,
      connectionId,
      expect.any(Object),
    );

    expect(onStart).toHaveBeenCalled();
    expect(onChunk).toHaveBeenCalledTimes(2);
    expect(onComplete).toHaveBeenCalled();

    expect(result.current.isStreaming).toBe(false);
    expect(result.current.streamedText).toBe("Hello World!");
  });

  it("should handle error state correctly", async () => {
    aiApi.streamTripChat.mockImplementation(
      async (tId, msg, connId, callbacks) => {
        callbacks.onError?.(new Error("Network Error"));
      },
    );

    const { result } = renderHook(() => useAITripChat("trip-123"));
    const onError = vi.fn();

    await act(async () => {
      await result.current.sendAIMessage("message", "conn", { onError });
    });

    expect(result.current.isStreaming).toBe(false);
    expect(onError).toHaveBeenCalled();
  });
});
