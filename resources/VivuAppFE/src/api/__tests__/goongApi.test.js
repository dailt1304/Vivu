import { describe, it, expect, vi, beforeEach } from "vitest";
import goongApi from "../goongApi";

describe("goongApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    globalThis.fetch = vi.fn();
  });

  describe("autocomplete", () => {
    it("should call fetch with correct URL when input is provided", async () => {
      const mockResponse = { predictions: [{ description: "Hanoi" }] };
      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await goongApi.autocomplete("Hanoi", "session123");

      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("/Place/AutoComplete?input=Hanoi"),
      );
      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("&sessiontoken=session123"),
      );
      expect(result).toEqual(mockResponse);
    });

    it("should return empty predictions when input is falsy", async () => {
      const result = await goongApi.autocomplete("");
      expect(globalThis.fetch).not.toHaveBeenCalled();
      expect(result).toEqual({ predictions: [] });
    });
  });

  describe("placeDetail", () => {
    it("should call fetch with correct URL when placeId is provided", async () => {
      const mockResponse = { result: { name: "Hanoi" } };
      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await goongApi.placeDetail("place123", "session123");

      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("/Place/Detail?place_id=place123"),
      );
      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("&sessiontoken=session123"),
      );
      expect(result).toEqual(mockResponse);
    });

    it("should return null when placeId is falsy", async () => {
      const result = await goongApi.placeDetail("");
      expect(globalThis.fetch).not.toHaveBeenCalled();
      expect(result).toBeNull();
    });
  });

  describe("direction", () => {
    it("should call fetch with correct URL when origin and destination are provided", async () => {
      const mockResponse = { routes: [] };
      globalThis.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await goongApi.direction("coord1", "coord2", "bike");

      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining(
          "/Direction?origin=coord1&destination=coord2&vehicle=bike",
        ),
      );
      expect(result).toEqual(mockResponse);
    });

    it("should return null when origin or destination is falsy", async () => {
      const result = await goongApi.direction("", "coord2");
      expect(global.fetch).not.toHaveBeenCalled();
      expect(result).toBeNull();
    });
  });
});
