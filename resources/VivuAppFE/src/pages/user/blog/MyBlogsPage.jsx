import React, { useState, useMemo, useCallback, lazy, Suspense } from "react";
import { useNavigate } from "react-router-dom";
import AppNavbar from "../../../components/layout/AppNavbar";
import MyBlogCard from "../../../components/common/cards/MyBlogCard";
import {
  useMyBlogs,
  useDeleteBlog,
  usePublishBlog,
} from "../../../hooks/blogs/useBlogs";
import { PenSquare, FileText, Filter, Sparkles, ArrowLeft } from "lucide-react";
import { mutate } from "swr";
import toast from "../../../utils/toast";
import { motion } from "framer-motion";

// Vercel Rule: bundle-dynamic-imports — lazy load modals
const DeleteBlogDialog = lazy(
  () => import("../../../components/user/blog/DeleteBlogDialog"),
);
const DraftPickerDialog = lazy(
  () => import("../../../components/user/blog/DraftPickerDialog"),
);

// Vercel Rule: rendering-hoist-jsx
const FILTER_TABS = [
  { key: "all", label: "Tất cả" },
  { key: "published", label: "Đã đăng" },
  { key: "draft", label: "Bản nháp" },
];

const ITEMS_PER_PAGE = 6;

/**
 * MyBlogsPage — Displays current user's blogs with full CRUD.
 * Vercel Rules: client-swr-dedup, rerender-derived-state-no-effect,
 *   rendering-content-visibility, rendering-conditional-render
 */
const MyBlogsPage = () => {
  const navigate = useNavigate();
  const [currentPage, setCurrentPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("all");
  const [deletingBlog, setDeletingBlog] = useState(null);
  const [publishingBlogId, setPublishingBlogId] = useState(null);

  const { data: blogsData, isLoading } = useMyBlogs(currentPage, 50);

  const { trigger: triggerDelete, isMutating: isDeleting } = useDeleteBlog();
  const { trigger: triggerPublish } = usePublishBlog();

  const [showDraftPicker, setShowDraftPicker] = useState(false);
  const myDrafts = useMemo(
    () =>
      (blogsData?.items || []).filter(
        (b) => b.status === "draft" && b.status?.toLowerCase() !== "deleted",
      ),
    [blogsData],
  );
  const hasDrafts = myDrafts.length > 0;

  const allBlogs = useMemo(
    () =>
      (blogsData?.items || []).filter(
        (b) => b.status?.toLowerCase() !== "deleted",
      ),
    [blogsData],
  );

  // Vercel Rule: rerender-derived-state-no-effect — derive during render
  const filteredBlogs = useMemo(() => {
    if (statusFilter === "published") {
      return allBlogs.filter((b) => !!b.publishedAt);
    }
    if (statusFilter === "draft") {
      return allBlogs.filter((b) => !b.publishedAt);
    }
    return allBlogs;
  }, [allBlogs, statusFilter]);

  // Paginate the filtered results
  const paginatedBlogs = useMemo(() => {
    const start = 0;
    const end = start + ITEMS_PER_PAGE;
    return filteredBlogs.slice(start, end);
  }, [filteredBlogs]);

  const totalPages = Math.ceil(filteredBlogs.length / ITEMS_PER_PAGE);

  const handleEdit = useCallback(
    (blog) => {
      navigate(`/inspiration/edit/${blog.id}`);
    },
    [navigate],
  );

  const handleView = useCallback(
    (blog) => {
      // Chỉ cho phép xem chi tiết nếu blog đã xuất bản (để tránh lỗi dữ liệu)
      // Nếu bạn muốn cho phép xem nháp, có thể bỏ điều kiện này
      navigate(`/inspiration/${blog.id}`);
    },
    [navigate],
  );

  const handleDelete = useCallback((blog) => {
    setDeletingBlog(blog);
  }, []);

  const handleDeleteConfirm = useCallback(
    async (blogId) => {
      try {
        await triggerDelete(blogId);
        setDeletingBlog(null);
        mutate((key) => Array.isArray(key) && key[0] === "my-blogs");
        toast.success("Đã xóa bài viết!");
      } catch {
        toast.error("Xóa bài viết thất bại!");
      }
    },
    [triggerDelete],
  );

  const handlePublish = useCallback(
    async (blog) => {
      try {
        setPublishingBlogId(blog.id);
        await triggerPublish(blog.id);
        mutate((key) => Array.isArray(key) && key[0] === "my-blogs");
        toast.success("Xuất bản thành công!");
      } catch {
        toast.error("Xuất bản thất bại!");
      } finally {
        setPublishingBlogId(null);
      }
    },
    [triggerPublish],
  );

  const handleFilterChange = useCallback((key) => {
    setStatusFilter(key);
    setCurrentPage(1);
  }, []);

  return (
    <div className="flex flex-col min-h-screen bg-gray-50 pb-bottom-nav lg:pb-0">
      <AppNavbar />

      <main className="flex-1 w-full max-w-[1600px] mx-auto px-4 sm:px-6 md:px-12 py-6 sm:py-10 space-y-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div className="flex flex-col">
            <button
              onClick={() => navigate("/inspiration")}
              className="flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 font-medium mb-1 transition-colors cursor-pointer w-fit"
            >
              <ArrowLeft size={14} />
              Quay lại Trải nghiệm
            </button>
            <h1 className="text-3xl sm:text-4xl lg:text-[40px] font-black text-gray-900 tracking-tight">
              Bài viết của tôi
            </h1>
          </div>

          <button
            onClick={() => {
              if (hasDrafts) {
                setShowDraftPicker(true);
              } else {
                navigate("/inspiration/create");
              }
            }}
            className="flex items-center justify-center gap-1.5 sm:gap-2 px-4 py-2 sm:px-5 sm:py-2.5 bg-gradient-primary text-white font-bold rounded-full hover:bg-blue-700 transition-all whitespace-nowrap cursor-pointer text-xs sm:text-sm"
          >
            <PenSquare size={16} />
            <span>Viết bài mới</span>
          </button>
        </div>

        {/* Filter Tabs & Search Row */}
        <div className="flex flex-col md:flex-row items-center gap-3 sm:gap-4 w-full border-b border-gray-100 pb-4">
          <div className="inline-flex bg-gray-100 p-1 rounded-full w-fit max-w-full overflow-x-auto no-scrollbar shrink-0 self-start shadow-inner relative">
            {FILTER_TABS.map((tab) => (
              <button
                key={tab.key}
                onClick={() => handleFilterChange(tab.key)}
                className={`relative z-10 flex px-6 py-2 rounded-full text-sm font-bold transition-colors items-center justify-center gap-2 cursor-pointer ${
                  statusFilter === tab.key
                    ? "text-gray-900"
                    : "text-gray-500 hover:text-gray-700 hover:bg-gray-50"
                }`}
              >
                {statusFilter === tab.key && (
                  <motion.div
                    layoutId="myBlogsTabPill"
                    className="absolute inset-0 bg-white rounded-full shadow-sm border border-gray-200 z-[-1]"
                    transition={{ type: "spring", bounce: 0.2, duration: 0.6 }}
                  />
                )}
                {tab.label}
              </button>
            ))}
          </div>
          <span className="ml-auto text-sm text-gray-500 font-medium hidden md:block">
            Có tất cả {filteredBlogs.length} bài viết
          </span>
        </div>

        {/* Blog Grid */}
        {/* Vercel Rule: rendering-content-visibility */}
        <div
          className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-6 pt-2"
          style={{ contentVisibility: "auto" }}
        >
          {isLoading ? (
            <div className="flex justify-center py-20 col-span-full">
              <div className="w-8 h-8 rounded-full border-4 border-gray-200 border-t-blue-500 animate-spin" />
            </div>
          ) : paginatedBlogs.length > 0 ? (
            paginatedBlogs.map((blog) => (
              <MyBlogCard
                key={blog.id}
                data={blog}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onPublish={handlePublish}
                onView={handleView}
                isPublishing={publishingBlogId === blog.id}
              />
            ))
          ) : (
            <div className="text-center py-20 col-span-full">
              <div className="w-20 h-20 mx-auto mb-4 rounded-full bg-blue-50 flex items-center justify-center">
                <Sparkles size={32} className="text-blue-400" />
              </div>
              <h3 className="text-lg font-bold text-gray-800 mb-2">
                {statusFilter === "all"
                  ? "Chưa có bài viết nào"
                  : statusFilter === "published"
                    ? "Chưa có bài viết nào đã đăng"
                    : "Không có bản nháp nào"}
              </h3>
              <p className="text-sm text-gray-500 mb-6">
                Hãy bắt đầu bằng cách tạo bài viết đầu tiên của bạn!
              </p>
              <button
                onClick={() => {
                  if (hasDrafts) {
                    setShowDraftPicker(true);
                  } else {
                    navigate("/inspiration/create");
                  }
                }}
                className="px-6 py-3 bg-gradient-primary text-white font-bold rounded-full shadow-md shadow-blue-200 hover:shadow-lg transition-all cursor-pointer"
              >
                <PenSquare size={16} className="inline mr-2" />
                Viết bài mới
              </button>
            </div>
          )}
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex justify-center items-center gap-2 mt-8">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors cursor-pointer"
            >
              &lt;
            </button>

            {[...Array(totalPages)].map((_, i) => {
              const pageNum = i + 1;
              if (
                pageNum === 1 ||
                pageNum === totalPages ||
                (pageNum >= currentPage - 1 && pageNum <= currentPage + 1)
              ) {
                return (
                  <button
                    key={pageNum}
                    onClick={() => setCurrentPage(pageNum)}
                    className={`w-10 h-10 flex items-center justify-center rounded-xl text-sm font-bold transition-all cursor-pointer ${
                      currentPage === pageNum
                        ? "bg-gradient-primary text-white shadow-md shadow-blue-500/20"
                        : "border border-gray-200 text-gray-600 hover:bg-gray-50 hover:text-blue-600"
                    }`}
                  >
                    {pageNum}
                  </button>
                );
              } else if (
                pageNum === currentPage - 2 ||
                pageNum === currentPage + 2
              ) {
                return (
                  <span key={pageNum} className="text-gray-400">
                    ...
                  </span>
                );
              }
              return null;
            })}

            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors cursor-pointer"
            >
              &gt;
            </button>
          </div>
        )}
      </main>

      {/* Modals — Vercel Rule: bundle-dynamic-imports */}
      <Suspense fallback={null}>
        <DeleteBlogDialog
          isOpen={!!deletingBlog}
          blog={deletingBlog}
          onClose={() => setDeletingBlog(null)}
          onConfirm={handleDeleteConfirm}
          isDeleting={isDeleting}
        />
        <DraftPickerDialog
          isOpen={showDraftPicker}
          drafts={myDrafts}
          onClose={() => setShowDraftPicker(false)}
        />
      </Suspense>
    </div>
  );
};

export default MyBlogsPage;
