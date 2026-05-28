import React, { useState, useMemo, useCallback } from "react";
import {
  Eye,
  MoreHorizontal,
  ThumbsUp,
  Trash2,
  Flag,
  Ban,
  FileWarning,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import DataTable from "@/components/cms/common/DataTable";
import StatusBadge from "@/components/cms/common/StatusBadge";
import DetailSheet from "@/components/cms/common/DetailSheet";
import ConfirmDialog from "@/components/cms/common/ConfirmDialog";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { usePublicBlogs, useBanBlog, useUnbanBlog } from "@/hooks/blogs/useBlogs";
import blogApi from "@/api/blogApi";
import toast from "@/utils/toast";
import { useLocation } from "react-router-dom";
import BlogReportsTab from "@/components/cms/blogs/BlogReportsTab";

const BlogManagementPage = () => {
  // ── Page-level Tab: "Danh sách bài viết" | "Báo cáo vi phạm" ──
  const [pageTab, setPageTab] = useState("blogs");

  const [activeTab, setActiveTab] = useState("all");
  const [selectedBlog, setSelectedBlog] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  
  const [banDialogOpen, setBanDialogOpen] = useState(false);
  const [blogToBan, setBlogToBan] = useState(null);
  
  const [unbanDialogOpen, setUnbanDialogOpen] = useState(false);
  const [blogToUnban, setBlogToUnban] = useState(null);

  const [sortConfig, setSortConfig] = useState({
    key: "publishedAt",
    direction: "desc",
  });
  const [page, setPage] = useState(1);

  const loc = useLocation();
  const isAdminTheme = [
    "/cms/dashboard",
    "/cms/users",
    "/cms/subscriptions",
  ].some((p) => loc.pathname.includes(p));
  const tabBorderClass = isAdminTheme
    ? "data-[state=active]:border-emerald-500"
    : "data-[state=active]:border-blue-500";

  // Fetch blogs from API
  const {
    data: blogsResponse,
    isLoading: loading,
    mutate,
  } = usePublicBlogs({
    sortBy:
      sortConfig.key === "publishedAt"
        ? "newest"
        : sortConfig.key === "viewCount"
          ? "most_viewed"
          : "newest",
    pageNumber: page,
    pageSize: 20,
    status: activeTab === "all" ? undefined : activeTab,
  });

  // Vercel Rule: rerender-move-effect-to-event — useSWRMutation for ban
  const { trigger: banBlog, isMutating: isBanning } = useBanBlog();
  const { trigger: unbanBlog, isMutating: isUnbanning } = useUnbanBlog();

  const blogsList = useMemo(() => {
    const items = blogsResponse?.items || blogsResponse || [];
    return Array.isArray(items) ? items : [];
  }, [blogsResponse]);

  // Vercel Rule: rerender-functional-setstate — useCallback for handlers
  const handleSort = useCallback((key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  }, []);

  const handleViewBlog = useCallback((blog) => {
    window.open(`/inspiration/${blog.id}`, "_blank");
  }, []);

  // Vercel Rule: rerender-memo — useMemo for columns
  const columns = useMemo(
    () => [
      {
        key: "title",
        label: "Tiêu đề bài viết",
        sortable: true,
        render: (value, row) => (
          <div className="max-w-[280px]">
            <p
              className="font-semibold text-zinc-900 truncate hover:text-emerald-600 transition-colors cursor-pointer"
              onClick={() => handleViewBlog(row)}
            >
              {value}
            </p>
            {row.shortDescription && (
              <p className="text-xs text-zinc-400 truncate mt-0.5">
                {row.shortDescription}
              </p>
            )}
          </div>
        ),
      },
      {
        key: "authorName",
        label: "Tác giả",
        render: (value, row) => (
          <div className="flex flex-col gap-1 py-1">
            <div className="flex items-center gap-2">
              <Avatar className="h-6 w-6 border border-zinc-200">
                <AvatarImage src={row.authorAvatarUrl} />
                <AvatarFallback className="bg-blue-50 text-blue-600 text-[10px]">
                  {(value || "U").charAt(0)}
                </AvatarFallback>
              </Avatar>
              <span className="text-sm font-medium text-zinc-800">
                {value || "Ẩn danh"}
              </span>
            </div>
          </div>
        ),
      },
      {
        key: "status",
        label: "Trạng thái",
        render: (value) => <StatusBadge status={value} />,
      },
      {
        key: "stats",
        label: "Tương tác",
        render: (_, row) => (
          <div className="flex items-center gap-3 text-xs text-zinc-500 font-[family-name:var(--font-cms-mono)]">
            <div className="flex items-center gap-1" title="Lượt xem">
              <Eye className="w-3.5 h-3.5" />{" "}
              {(row.viewCount || 0).toLocaleString()}
            </div>
            <div className="flex items-center gap-1" title="Lượt thích">
              <ThumbsUp className="w-3.5 h-3.5" />{" "}
              {(row.likeCount || 0).toLocaleString()}
            </div>
          </div>
        ),
      },
      {
        key: "publishedAt",
        label: "Ngày đăng",
        sortable: true,
        render: (value) => {
          if (!value)
            return <span className="text-xs text-zinc-400">—</span>;
          return (
            <span className="font-[family-name:var(--font-cms-mono)] text-sm text-zinc-600">
              {new Date(value).toLocaleDateString("vi-VN")}
            </span>
          );
        },
      },
    ],
    [handleViewBlog],
  );

  // Vercel Rule: rerender-memo — filter data
  const filteredData = useMemo(() => {
    let result = [...blogsList];

    if (activeTab !== "all") {
      result = result.filter(
        (blog) => (blog.status || "").toLowerCase() === activeTab,
      );
    }

    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (blog) =>
          (blog.title || "").toLowerCase().includes(q) ||
          (blog.authorName || "").toLowerCase().includes(q),
      );
    }

    return result;
  }, [activeTab, searchQuery, blogsList]);

  const handleUnbanClick = useCallback(
    (blog) => {
      setBlogToUnban(blog);
      setUnbanDialogOpen(true);
      if (sheetOpen) setSheetOpen(false);
    },
    [sheetOpen],
  );

  const handleConfirmUnban = useCallback(async () => {
    if (!blogToUnban) return;
    try {
      await unbanBlog(blogToUnban.id);
      mutate();
      toast.success(`Đã bỏ ẩn bài viết: ${blogToUnban.title}`);
    } catch (error) {
      console.error("Unban blog failed:", error);
      toast.error(
        error?.response?.data?.message ||
          "Bỏ ẩn bài viết thất bại. Vui lòng thử lại.",
      );
    } finally {
      setUnbanDialogOpen(false);
      setBlogToUnban(null);
    }
  }, [blogToUnban, mutate, unbanBlog]);

  const handleBanClick = useCallback(
    (blog) => {
      setBlogToBan(blog);
      setBanDialogOpen(true);
      if (sheetOpen) setSheetOpen(false);
    },
    [sheetOpen],
  );

  const handleConfirmBan = useCallback(async () => {
    if (!blogToBan) return;
    try {
      await banBlog(blogToBan.id);
      mutate();
      toast.success(`Đã ẩn bài viết: ${blogToBan.title}`);
    } catch (error) {
      console.error("Ban blog failed:", error);
      toast.error(
        error?.response?.data?.message ||
          "Ẩn bài viết thất bại. Vui lòng thử lại.",
      );
    } finally {
      setBanDialogOpen(false);
      setBlogToBan(null);
    }
  }, [blogToBan, banBlog, mutate]);

  // Vercel Rule: rerender-functional-setstate — useCallback for renderActions
  const renderActions = useCallback(
    (row) => (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="group-hover:bg-zinc-200/50"
          >
            <MoreHorizontal className="h-4 w-4 text-zinc-500" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuItem
            onClick={() => handleViewBlog(row)}
            className="cursor-pointer"
          >
            <Eye className="h-4 w-4 mr-2" />
            Xem chi tiết
          </DropdownMenuItem>
          <DropdownMenuSeparator className="bg-zinc-100" />
          {["hidden", "banned"].includes((row.status || "").toLowerCase()) ? (
            <DropdownMenuItem
              onClick={() => handleUnbanClick(row)}
              className="text-emerald-600 focus:text-emerald-700 focus:bg-emerald-50 cursor-pointer"
            >
              <ThumbsUp className="h-4 w-4 mr-2" />
              Bỏ ẩn bài viết
            </DropdownMenuItem>
          ) : (
            <DropdownMenuItem
              onClick={() => handleBanClick(row)}
              className="text-amber-600 focus:text-amber-700 focus:bg-amber-50 cursor-pointer"
            >
              <Ban className="h-4 w-4 mr-2" />
              Ẩn bài viết
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    ),
    [handleViewBlog, handleBanClick, handleUnbanClick],
  );

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-6">
        {/* Header */}
        <div className="border-b border-zinc-200 pb-6">
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">
            Quản lý blog
          </h1>
          <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
            Quản lý bài viết và xử lý các báo cáo vi phạm từ cộng đồng
          </p>
        </div>

        {/* Page Level Tabs */}
        <div className="flex items-center gap-1 bg-zinc-100 p-1 rounded-xl w-fit">
          <button
            onClick={() => setPageTab("blogs")}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition-all ${
              pageTab === "blogs"
                ? "bg-white text-zinc-900 shadow-sm"
                : "text-zinc-500 hover:text-zinc-700"
            }`}
          >
            Danh sách bài viết
          </button>
          <button
            onClick={() => setPageTab("reports")}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition-all flex items-center gap-2 ${
              pageTab === "reports"
                ? "bg-white text-zinc-900 shadow-sm"
                : "text-zinc-500 hover:text-zinc-700"
            }`}
          >
            <FileWarning className="h-4 w-4" />
            Báo cáo vi phạm
          </button>
        </div>

        {/* TAB 1: Blog List */}
        {pageTab === "blogs" && (
          <>
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <Tabs
                value={activeTab}
                onValueChange={setActiveTab}
                className="bg-transparent border-b border-zinc-200 pb-px w-full"
              >
                <TabsList className="bg-transparent p-0 h-auto gap-6 pb-2 overflow-x-auto justify-start whitespace-nowrap hide-scrollbar">
                  <TabsTrigger
                    value="all"
                    className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 ${tabBorderClass} rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium`}
                  >
                    Tất cả bài viết
                  </TabsTrigger>
                  <TabsTrigger
                    value="hidden"
                    className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 ${tabBorderClass} rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium`}
                  >
                    Đã ẩn
                  </TabsTrigger>
                </TabsList>
              </Tabs>
            </div>

            <div>
              <DataTable
                columns={columns}
                data={filteredData}
                loading={loading}
                searchPlaceholder="Tìm kiếm theo tiêu đề bài viết hoặc tên tác giả..."
                onSearch={setSearchQuery}
                sortConfig={sortConfig}
                onSort={handleSort}
                actions={renderActions}
                pagination={{
                  currentPage: 1,
                  totalPages: 1,
                  from: 1,
                  to: filteredData.length,
                  total: filteredData.length,
                }}
                emptyMessage="Không tìm thấy bài viết nào."
              />
            </div>

            {/* Detail Sheet */}
            <DetailSheet
              open={sheetOpen}
              onOpenChange={setSheetOpen}
              title="Chi tiết bài viết"
              description={`ID: ${selectedBlog?.id}`}
              className="sm:max-w-2xl"
            >
              {selectedBlog && sheetOpen ? (
                <div className="space-y-8 pb-10">
                  <div className="space-y-4">
                    <h2 className="text-2xl font-bold text-zinc-900 leading-tight">
                      {selectedBlog.title}
                    </h2>
                    <div className="flex flex-wrap items-center gap-4 text-sm text-zinc-500">
                      <div className="flex items-center gap-2">
                        <Avatar className="h-6 w-6">
                          <AvatarFallback className="bg-zinc-100 text-[10px]">
                            {selectedBlog.authorName?.charAt(0) || "U"}
                          </AvatarFallback>
                        </Avatar>
                        <span className="font-medium text-zinc-700">
                          {selectedBlog.authorName || "Ẩn danh"}
                        </span>
                      </div>
                      <span>•</span>
                      <span className="font-[family-name:var(--font-cms-mono)] text-xs">
                        {new Date(selectedBlog.createdAt).toLocaleDateString(
                          "vi-VN",
                          { dateStyle: "long" },
                        )}
                      </span>
                      <span>•</span>
                      <StatusBadge status={selectedBlog.status} />
                    </div>
                  </div>

                  <div className="p-6 bg-zinc-50 border border-zinc-100 rounded-xl leading-relaxed text-zinc-800 space-y-4">
                    {selectedBlog.shortDescription && (
                      <p className="italic text-zinc-500">
                        {selectedBlog.shortDescription}
                      </p>
                    )}
                    <div className="pt-4 border-t border-zinc-200 mt-4">
                      <h5 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-3">
                        Tags
                      </h5>
                      <div className="flex flex-wrap gap-2">
                        {selectedBlog.tags?.map((t, idx) => (
                          <span
                            key={idx}
                            className="bg-white border border-zinc-200 px-3 py-1 rounded-full text-xs font-medium text-zinc-600"
                          >
                            {typeof t === "string" ? t : t.name}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  {/* Moderator Actions */}
                  <div className="flex gap-2">
                    {["hidden", "banned"].includes((selectedBlog.status || "").toLowerCase()) ? (
                      <Button
                        variant="outline"
                        onClick={() => handleUnbanClick(selectedBlog)}
                        className="w-full border-emerald-200 hover:bg-emerald-50 text-emerald-600 hover:text-emerald-700"
                      >
                        <ThumbsUp className="h-4 w-4 mr-2" />
                        Bỏ ẩn bài viết
                      </Button>
                    ) : (
                      <Button
                        variant="outline"
                        onClick={() => handleBanClick(selectedBlog)}
                        className="w-full border-amber-200 hover:bg-amber-50 text-amber-600 hover:text-amber-700"
                      >
                        <Ban className="h-4 w-4 mr-2" />
                        Ẩn bài viết
                      </Button>
                    )}
                  </div>
                </div>
              ) : null}
            </DetailSheet>

            {/* Unban Confirmation */}
            <ConfirmDialog
              open={unbanDialogOpen}
              onOpenChange={setUnbanDialogOpen}
              title="Bỏ ẩn bài viết"
              description={`Bạn có chắc chắn muốn bỏ ẩn bài viết "${blogToUnban?.title}"? Bài viết sẽ được hiển thị lại công khai.`}
              onConfirm={handleConfirmUnban}
              loading={isUnbanning}
              confirmText="Bỏ ẩn bài viết"
              cancelText="Hủy bỏ"
            />

            {/* Ban Confirmation */}
            <ConfirmDialog
              open={banDialogOpen}
              onOpenChange={setBanDialogOpen}
              title="Ẩn bài viết"
              description={`Bạn có chắc chắn muốn ẩn bài viết "${blogToBan?.title}"? Bài viết sẽ không hiển thị cho người dùng.`}
              onConfirm={handleConfirmBan}
              loading={isBanning}
              isDestructive={true}
              confirmText="Ẩn bài viết"
              cancelText="Hủy bỏ"
            />
          </>
        )}

        {/* TAB 2: Blog Reports */}
        {pageTab === "reports" && <BlogReportsTab />}
      </div>
    </div>
  );
};

export default BlogManagementPage;
