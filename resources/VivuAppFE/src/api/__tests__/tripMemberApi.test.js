import { describe, it, expect, vi, beforeEach } from "vitest";
import tripMemberApi from "../tripMemberApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
    patch: vi.fn(),
  },
}));

describe("tripMemberApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getMembers", () => {
    it("should call axiosClient.get with correct URL and default params", async () => {
      const mockResponse = { data: { items: [] } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await tripMemberApi.getMembers("t1");

      expect(axiosClient.get).toHaveBeenCalledWith("/tripmembers/t1", {
        params: { pageNumber: 1, pageSize: 20 },
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("removeMember", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await tripMemberApi.removeMember("t1", "u2");

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/tripmembers/remove-member",
        {
          tripId: "t1",
          userId: "u2",
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("leaveTrip", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const result = await tripMemberApi.leaveTrip("t1");

      expect(axiosClient.delete).toHaveBeenCalledWith("/tripmembers/t1/leave");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("updateMemberRole", () => {
    it("should call axiosClient.patch with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.patch.mockResolvedValueOnce(mockResponse);

      const result = await tripMemberApi.updateMemberRole("t1", "u2", "Editor");

      expect(axiosClient.patch).toHaveBeenCalledWith("/trips/t1/members/u2", {
        newRole: "Editor",
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("joinTrip", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const result = await tripMemberApi.joinTrip("ABC123");

      expect(axiosClient.post).toHaveBeenCalledWith("/trips/join", {
        inviteCode: "ABC123",
      });
      expect(result).toEqual(mockResponse);
    });
  });
});
