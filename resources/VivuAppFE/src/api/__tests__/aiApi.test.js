import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import aiApi from "../aiApi";

// Mock environment variables
vi.stubEnv("VITE_API_URL", "http://localhost:7294/api");

describe("aiApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Kết hợp mock fetch từ cả 2 nguồn
    globalThis.fetch = vi.fn();
    vi.stubGlobal("fetch", globalThis.fetch);

    // Kết hợp mock localStorage (đồng nhất dùng chung 'mock-token')
    const mockLocalStorage = {
      getItem: vi.fn().mockReturnValue("mock-token"),
    };
    Object.defineProperty(window, "localStorage", {
      value: mockLocalStorage,
      writable: true,
    });
    Storage.prototype.getItem = vi.fn(() => "mock-token");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe("streamGenerateTrip", () => {
    it("should call fetch with correct URL, headers, and body", async () => {
      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: {
          getReader: () => ({
            read: vi.fn().mockResolvedValueOnce({ done: true }),
          }),
        },
      });

      const callbacks = {};
      await aiApi.streamGenerateTrip("Test prompt", callbacks, {
        autoSave: true,
      });

      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("/ai/stream/generate-trip"),
        expect.objectContaining({
          method: "POST",
          headers: expect.objectContaining({
            Authorization: "Bearer mock-token",
            "Content-Type": "application/json",
          }),
          body: JSON.stringify({ userPrompt: "Test prompt", autoSave: true }),
        }),
      );
    });

    it("should call fetch with correct generate-trip URL and payload", async () => {
      const mockReader = {
        read: vi
          .fn()
          .mockResolvedValue({ done: true, value: new Uint8Array() }),
      };

      globalThis.fetch.mockResolvedValue({
        ok: true,
        body: {
          getReader: () => mockReader,
        },
      });

      const userPrompt = "Create a 3 day trip to Paris";

      await aiApi.streamGenerateTrip(userPrompt, {}, { autoSave: true });

      expect(globalThis.fetch).toHaveBeenCalledWith(
        `http://localhost:7294/api/ai/stream/generate-trip`,
        expect.objectContaining({
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: "Bearer mock-token",
          },
          body: JSON.stringify({ userPrompt, autoSave: true }),
        }),
      );
    });

    it("should emit onStart event", async () => {
      const encoder = new TextEncoder();
      const mockStream = new ReadableStream({
        start(controller) {
          controller.enqueue(encoder.encode("event: start\ndata: {}\n\n"));
          controller.close();
        },
      });

      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: mockStream,
      });

      const onStart = vi.fn();
      await aiApi.streamGenerateTrip("prompt", { onStart });

      expect(onStart).toHaveBeenCalledWith({});
    });

    it("should emit onChunk event", async () => {
      const encoder = new TextEncoder();
      const mockStream = new ReadableStream({
        start(controller) {
          controller.enqueue(
            encoder.encode("event: chunk\ndata: chunk data\n\n"),
          );
          controller.close();
        },
      });

      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: mockStream,
      });

      const onChunk = vi.fn();
      await aiApi.streamGenerateTrip("prompt", { onChunk });

      expect(onChunk).toHaveBeenCalledWith("chunk data");
    });

    it("should normalize chunk fences and process multiple events from one read", async () => {
      const encoder = new TextEncoder();
      const mockStream = new ReadableStream({
        start(controller) {
          controller.enqueue(
            encoder.encode(
              'event: start\r\ndata: {}\r\n\r\nevent: chunk\r\ndata: ```json\r\ndata: {\r\ndata:   "title": "Hello"\r\n\r\n',
            ),
          );
          controller.close();
        },
      });

      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: mockStream,
      });

      const onStart = vi.fn();
      const onChunk = vi.fn();

      await aiApi.streamGenerateTrip("prompt", { onStart, onChunk });

      expect(onStart).toHaveBeenCalledWith({});
      expect(onChunk).toHaveBeenCalledWith(
        expect.stringContaining('"title": "Hello"'),
      );
      expect(onChunk.mock.calls[0][0]).not.toContain("```");
    });

    it("should join multi-line data and flush trailing buffered event on stream end", async () => {
      const encoder = new TextEncoder();
      const mockStream = new ReadableStream({
        start(controller) {
          controller.enqueue(
            encoder.encode(
              'event: complete\ndata: {"status":\n' +
                'data: "done"}\n\n' +
                'event: saved\ndata: {"id":"trip-1"}',
            ),
          );
          controller.close();
        },
      });

      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: mockStream,
      });

      const onComplete = vi.fn();
      const onSaved = vi.fn();

      await aiApi.streamGenerateTrip("prompt", { onComplete, onSaved });

      expect(onComplete).toHaveBeenCalledWith({ status: "done" });
      expect(onSaved).toHaveBeenCalledWith({ id: "trip-1" });
    });

    it("should emit onComplete event with parsed JSON", async () => {
      const encoder = new TextEncoder();
      const mockStream = new ReadableStream({
        start(controller) {
          controller.enqueue(
            encoder.encode('event: complete\ndata: {"status": "done"}\n\n'),
          );
          controller.close();
        },
      });

      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        body: mockStream,
      });

      const onComplete = vi.fn();
      await aiApi.streamGenerateTrip("prompt", { onComplete });

      expect(onComplete).toHaveBeenCalledWith({ status: "done" });
    });

    it("should emit onError when HTTP status is not ok", async () => {
      globalThis.fetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const onError = vi.fn();
      await aiApi.streamGenerateTrip("prompt", { onError });

      expect(onError).toHaveBeenCalledWith("HTTP error! status: 500");
    });

    it("should emit onError when fetch throws an error", async () => {
      globalThis.fetch.mockRejectedValueOnce(new Error("Network Error"));

      const onError = vi.fn();
      await aiApi.streamGenerateTrip("prompt", { onError });

      expect(onError).toHaveBeenCalledWith("Network Error");
    });
  });

  describe("streamTripChat", () => {
    it("should call fetch with correct URL including connectionId and payload", async () => {
      const mockReader = {
        read: vi
          .fn()
          .mockResolvedValue({ done: true, value: new Uint8Array() }),
      };

      globalThis.fetch.mockResolvedValue({
        ok: true,
        body: {
          getReader: () => mockReader,
        },
      });

      const tripId = "trip-123";
      const message = "Suggest a hotel";
      const connectionId = "conn-xyz";

      await aiApi.streamTripChat(tripId, message, connectionId, {});

      expect(globalThis.fetch).toHaveBeenCalledWith(
        `http://localhost:7294/api/ai/trips/${tripId}/chat?connectionId=${connectionId}`,
        expect.objectContaining({
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: "Bearer mock-token",
          },
          body: JSON.stringify({ userMessage: message }),
        }),
      );
    });
  });
});
