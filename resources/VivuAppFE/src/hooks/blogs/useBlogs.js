import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import blogApi from "../../api/blogApi";
import { useDebounce } from "../utils";

/**
 * Custom hooks for Managing Blogs
 * Vercel Rules Applied:
 * - client-swr-dedup: useSWR for caching and deduplication
 * - rerender-move-effect-to-event: useSWRMutation for event-driven API calls
 */

// ── READ HOOKS ──────────────────────────────────────────────────────────────

export function usePublicBlogs(params) {
  // Pass params object to the key array so SWR caches uniquely per query
  const key = params ? ["public-blogs", params] : null;
  return useSWR(key, () => blogApi.getAll(params).then((res) => res.data));
}

/**
 * Hook tìm kiếm blogs công khai với auto-debounce
 * - Chỉ gọi API khi searchTerm >= 2 ký tự (tránh spam request)
 * - SWR tự cache kết quả đã search (gõ lại = instant)
 * - Debounce 400ms để user gõ xong mới fetch
 */
export function useSearchPublicBlogs(searchTerm, pageSize = 15, pageNumber = 1) {
  const debouncedTerm = useDebounce(searchTerm, 400);

  // SWR key = null khi chưa đủ ký tự -> không fetch
  const key =
    debouncedTerm?.length >= 2
      ? ["search-public-blogs", debouncedTerm, pageSize, pageNumber]
      : null;

  return useSWR(
    key,
    () =>
      blogApi
        .searchPublic({ q: debouncedTerm, pageSize, pageNumber })
        .then((r) => r.data),
    { keepPreviousData: true },
  );
}

export function useBlogDetail(idOrSlug) {
  return useSWR(idOrSlug ? ["blog-detail", idOrSlug] : null, ([, id]) =>
    blogApi.getByIdOrSlug(id).then((res) => res.data),
    {
      shouldRetryOnError: false, // Ngăn chặn vòng lặp retry nếu gặp lỗi 400 (như khi lấy draft nhưng không phải tác giả)
    }
  );
}

// Vercel Rule: client-swr-dedup — SWR auto cache + dedup
// Use primitive values in key to prevent infinite re-fetch loops
export function useMyBlogs(pageNumber, pageSize) {
  const key = pageNumber != null ? ["my-blogs", pageNumber, pageSize] : null;
  return useSWR(key, () =>
    blogApi.getMyBlogs({ pageNumber, pageSize }).then((res) => res.data),
  );
}

export function useMyDrafts() {
  return useSWR("my-drafts", () =>
    blogApi.getMyDrafts().then((res) => res.data),
  );
}

export function useMyBookmarks(pageNumber, pageSize) {
  const key =
    pageNumber != null ? ["my-bookmarks", pageNumber, pageSize] : null;
  return useSWR(key, () =>
    blogApi.getMyBookmarks({ pageNumber, pageSize }).then((res) => res.data),
  );
}

export function useBlogComments(blogId, pageNumber = 1, pageSize = 20) {
  const key = blogId ? ["blog-comments", blogId, pageNumber, pageSize] : null;
  return useSWR(key, () =>
    blogApi
      .getComments(blogId, { pageNumber, pageSize })
      .then((res) => res.data),
  );
}

// ── MUTATION HOOKS ──────────────────────────────────────────────────────────

export function useCreateBlog() {
  return useSWRMutation("create-blog", (_, { arg: formData }) =>
    blogApi.createFromTrip(formData).then((res) => res.data),
  );
}

export function useUpdateBlog() {
  return useSWRMutation("update-blog", (_, { arg: { blogId, formData } }) =>
    blogApi.update(blogId, formData).then((res) => res.data),
  );
}

export function useDeleteBlog() {
  return useSWRMutation("delete-blog", (_, { arg: blogId }) =>
    blogApi.delete(blogId).then((res) => res.data),
  );
}

export function usePublishBlog() {
  return useSWRMutation("publish-blog", (_, { arg: blogId }) =>
    blogApi.publish(blogId).then((res) => res.data),
  );
}

export function useLikeBlog() {
  return useSWRMutation("like-blog", (_, { arg: blogId }) =>
    blogApi.like(blogId).then((res) => res.data),
  );
}

export function useBookmarkBlog() {
  return useSWRMutation("bookmark-blog", (_, { arg: blogId }) =>
    blogApi.bookmark(blogId).then((res) => res.data),
  );
}

export function useCreateComment() {
  return useSWRMutation(
    "create-comment",
    (_, { arg: { blogId, content, parentCommentId } }) =>
      blogApi
        .createComment(blogId, { blogId, content, parentCommentId })
        .then((res) => res.data),
  );
}

export function useDeleteComment() {
  return useSWRMutation("delete-comment", (_, { arg: { blogId, commentId } }) =>
    blogApi.deleteComment(blogId, commentId).then((res) => res.data),
  );
}

// ── MODERATION HOOKS ────────────────────────────────────────────────────

export function useReportBlog() {
  return useSWRMutation("report-blog", (_, { arg: { blogId, data } }) =>
    blogApi.reportBlog(blogId, data).then((res) => res.data),
  );
}

export function useFlagBlog() {
  return useSWRMutation("flag-blog", (_, { arg: { blogId, data } }) =>
    blogApi.flagBlog(blogId, data).then((res) => res.data),
  );
}

export function useBanBlog() {
  return useSWRMutation("ban-blog", (_, { arg: blogId }) =>
    blogApi.banBlog(blogId).then((res) => res.data),
  );
}

export function useUnbanBlog() {
  return useSWRMutation("unban-blog", (_, { arg: blogId }) =>
    blogApi.unbanBlog(blogId).then((res) => res.data),
  );
}
