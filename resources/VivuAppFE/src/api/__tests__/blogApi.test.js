import { describe, it, expect, vi, beforeEach } from "vitest";
import blogApi from "../blogApi";
import axiosClient from "../axiosClient";

// Mock the axios client
vi.mock("../axiosClient", () => {
  return {
    default: {
      post: vi.fn(),
      delete: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      get: vi.fn(),
    },
  };
});

describe("blogApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getAll", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 12, sortBy: "newest" };
      const result = await blogApi.getAll(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/blogs", { params });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getByIdOrSlug", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { id: "blog-1", title: "Test" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const idOrSlug = "blog-1";
      const result = await blogApi.getByIdOrSlug(idOrSlug);

      expect(axiosClient.get).toHaveBeenCalledWith(`/blogs/${idOrSlug}`);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("createFromTrip", () => {
    it("should call axiosClient.post with correct URL, formData, and headers", async () => {
      const mockResponse = { data: { id: "new-blog-1" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const formData = new FormData();
      formData.append("TripId", "trip-1");
      const result = await blogApi.createFromTrip(formData);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/blogs/from-trip",
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("update", () => {
    it("should call axiosClient.put with correct URL, formData, and headers", async () => {
      const mockResponse = { data: { id: "blog-1" } };
      axiosClient.put.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const formData = new FormData();
      formData.append("Title", "Updated Title");
      const result = await blogApi.update(blogId, formData);

      expect(axiosClient.put).toHaveBeenCalledWith(
        `/blogs/${blogId}`,
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("delete", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const result = await blogApi.delete(blogId);

      expect(axiosClient.delete).toHaveBeenCalledWith(`/blogs/${blogId}`);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("publish", () => {
    it("should call axiosClient.patch with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.patch.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const result = await blogApi.publish(blogId);

      expect(axiosClient.patch).toHaveBeenCalledWith(
        `/blogs/${blogId}/publish`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("like", () => {
    it("should call axiosClient.post with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const result = await blogApi.like(blogId);

      expect(axiosClient.post).toHaveBeenCalledWith(`/blogs/${blogId}/like`);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("bookmark", () => {
    it("should call axiosClient.post with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const result = await blogApi.bookmark(blogId);

      expect(axiosClient.post).toHaveBeenCalledWith(
        `/blogs/${blogId}/bookmark`,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getMyBookmarks", () => {
    it("should call axiosClient.get with correct URL and params", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const params = { pageNumber: 1, pageSize: 12 };
      const result = await blogApi.getMyBookmarks(params);

      expect(axiosClient.get).toHaveBeenCalledWith("/blogs/me/bookmarks", {
        params,
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getComments", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { items: [], totalCount: 0 } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const params = { pageNumber: 1, pageSize: 10 };
      const result = await blogApi.getComments(blogId, params);

      expect(axiosClient.get).toHaveBeenCalledWith(
        `/blogs/${blogId}/comments`,
        {
          params,
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("createComment", () => {
    it("should call axiosClient.post with correct URL and body", async () => {
      const mockResponse = { data: { id: "comm-1" } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const data = { content: "Nice blog!" };
      const result = await blogApi.createComment(blogId, data);

      expect(axiosClient.post).toHaveBeenCalledWith(
        `/blogs/${blogId}/comments`,
        data,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("deleteComment", () => {
    it("should call axiosClient.delete with correct URL", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.delete.mockResolvedValueOnce(mockResponse);

      const blogId = "blog-1";
      const commentId = "comm-1";
      const result = await blogApi.deleteComment(blogId, commentId);

      expect(axiosClient.delete).toHaveBeenCalledWith(
        `/blogs/${blogId}/comments/${commentId}`,
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
