import { renderHook, act, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { useTripChat } from "../useTripChat";

const { mockInvoke, mockOn, mockOff, mockStop, mockStart, mockConnection } =
  vi.hoisted(() => {
    const mockInvoke = vi.fn().mockResolvedValue(undefined);
    const mockOn = vi.fn();
    const mockOff = vi.fn();
    const mockStop = vi.fn();
    const mockStart = vi.fn().mockResolvedValue(undefined);

    return {
      mockInvoke,
      mockOn,
      mockOff,
      mockStop,
      mockStart,
      mockConnection: {
        invoke: mockInvoke,
        on: mockOn,
        off: mockOff,
        stop: mockStop,
        start: mockStart,
        state: "Connected",
      },
    };
  });

vi.mock("@microsoft/signalr", () => {
  return {
    HubConnectionBuilder: vi.fn().mockImplementation(function () {
      return {
        withUrl: vi.fn().mockReturnThis(),
        withAutomaticReconnect: vi.fn().mockReturnThis(),
        build: vi.fn().mockReturnValue(mockConnection),
      };
    }),
    HubConnectionState: {
      Connected: "Connected",
      Disconnected: "Disconnected",
    },
  };
});

describe("useTripChat", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should initialize and connect to SignalR hub", async () => {
    const tripId = "trip-123";
    const accessToken = "fake-token";
    const onMessageReceived = vi.fn();

    renderHook(() => useTripChat(tripId, accessToken, { onMessageReceived }));

    await waitFor(() => {
      expect(mockStart).toHaveBeenCalled();
    });

    expect(mockOn).toHaveBeenCalledWith("ReceiveMessage", expect.any(Function));
    expect(mockOn).toHaveBeenCalledWith("ReceiveAIChunk", expect.any(Function));
    expect(mockOn).toHaveBeenCalledWith(
      "ReceiveAIMessage",
      expect.any(Function),
    );
    expect(mockOn).toHaveBeenCalledWith("TripUpdated", expect.any(Function));
    expect(mockInvoke).toHaveBeenCalledWith("JoinTrip", tripId);
  });

  it("should send message correctly", async () => {
    const tripId = "trip-123";
    const accessToken = "fake-token";

    const { result } = renderHook(() =>
      useTripChat(tripId, accessToken, { onMessageReceived: vi.fn() }),
    );

    await waitFor(() => {
      expect(result.current.isConnected).toBe(true);
    });

    await act(async () => {
      await result.current.sendMessage("Hello Group", "text", null);
    });

    expect(mockInvoke).toHaveBeenCalledWith("SendMessage", {
      tripId: "trip-123",
      content: "Hello Group",
      messageType: "text",
      replyToId: null,
    });
  });

  it("should clean up connection on unmount", async () => {
    const { unmount } = renderHook(() =>
      useTripChat("trip-123", "fake-token", { onMessageReceived: vi.fn() }),
    );

    await waitFor(() => {
      expect(mockStart).toHaveBeenCalled();
    });

    unmount();

    expect(mockInvoke).toHaveBeenCalledWith("LeaveTrip", "trip-123");
    expect(mockOff).toHaveBeenCalledWith("ReceiveMessage");
    expect(mockOff).toHaveBeenCalledWith("ReceiveAIChunk");
    expect(mockOff).toHaveBeenCalledWith("ReceiveAIMessage");
    expect(mockOff).toHaveBeenCalledWith("TripUpdated");
    expect(mockStop).toHaveBeenCalled();
  });
});
