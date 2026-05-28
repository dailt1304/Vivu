import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook } from "@testing-library/react";
import { useLikeBlog, useBookmarkBlog, useBlogComments } from "../useBlogs";
import blogApi from "../../../api/blogApi";

// Mock dependencies
vi.mock("../../../api/blogApi", () => ({
  default: {
    like: vi.fn(),
    bookmark: vi.fn(),
    getComments: vi.fn(),
  },
}));

// Mock SWR mutation return values
vi.mock("swr/mutation", () => ({
  default: vi.fn((key, fetcher) => ({
    trigger: (arg) => fetcher(key, { arg }),
    isMutating: false,
  })),
}));

// Mock SWR return values
vi.mock("swr", () => ({
  default: vi.fn((key) => ({
    data:
      Array.isArray(key) && key[0].includes("comments")
        ? { items: [{ id: "c1", content: "test" }] }
        : null,
    isLoading: false,
    mutate: vi.fn(),
  })),
}));

describe("useBlogs hooks", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("useLikeBlog", () => {
    it("should trigger blogApi.like when trigger is called", async () => {
      blogApi.like.mockResolvedValueOnce({ data: { success: true } });
      const { result } = renderHook(() => useLikeBlog());

      await result.current.trigger("blog-1");
      expect(blogApi.like).toHaveBeenCalledWith("blog-1");
    });
  });

  describe("useBookmarkBlog", () => {
    it("should trigger blogApi.bookmark when trigger is called", async () => {
      blogApi.bookmark.mockResolvedValueOnce({ data: { success: true } });
      const { result } = renderHook(() => useBookmarkBlog());

      await result.current.trigger("blog-1");
      expect(blogApi.bookmark).toHaveBeenCalledWith("blog-1");
    });
  });

  describe("useBlogComments", () => {
    it("should fetch comments and return data", async () => {
      const { result } = renderHook(() => useBlogComments("blog-1"));

      expect(result.current.data).toBeDefined();
      expect(result.current.data.items[0].content).toBe("test");
    });
  });
});
