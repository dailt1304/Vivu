import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import blogReportApi from "../../api/blogReportApi";

/**
 * Hooks for Blog Report Management (CMS Moderator)
 * Vercel Rules Applied:
 * - client-swr-dedup: useSWR for caching and deduplication
 * - rerender-move-effect-to-event: useSWRMutation for event-driven API calls
 */

// 1. Hook lấy danh sách blog reports (Moderator)
export function useBlogReports(params) {
  const key = [
    "blog-reports",
    params?.pageNumber,
    params?.pageSize,
    params?.status,
    params?.reportType,
  ];

  return useSWR(key, () =>
    blogReportApi.getAll(params).then((res) => res.data),
  );
}

// 2. Hook xử lý blog report (Approve/Reject)
export function useReviewBlogReport() {
  return useSWRMutation("review-blog-report", (_, { arg: data }) =>
    blogReportApi.review(data).then((res) => res.data),
  );
}
