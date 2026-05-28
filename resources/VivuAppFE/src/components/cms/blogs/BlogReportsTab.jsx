import React, { useState, useMemo, useCallback } from "react";
import {
  Eye,
  MoreHorizontal,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Ban,
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
import {
  useBlogReports,
  useReviewBlogReport,
} from "@/hooks/blogs/useBlogReports";
import { useBanBlog } from "@/hooks/blogs/useBlogs";
import toast from "@/utils/toast";
import { cn } from "@/lib/utils";

const REPORT_TYPE_LABELS = {
  SPAM: "Spam",
  INAPPROPRIATE: "Không phù hợp",
  MISLEADING: "Sai lệch",
  COPYRIGHT: "Bản quyền",
  OTHER: "Khác",
};

const BlogReportsTab = () => {
  const [activeTab, setActiveTab] = useState("PENDING");
  const [selectedReport, setSelectedReport] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [banDialogOpen, setBanDialogOpen] = useState(false);
  const [reportToProcess, setReportToProcess] = useState(null);
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);

  // Vercel Rule: client-swr-dedup — SWR auto cache + dedup
  const {
    data: reportsRes,
    isLoading: loading,
    mutate,
  } = useBlogReports({
    pageNumber: 1,
    pageSize: 100,
    status: activeTab === "all" ? undefined : activeTab,
  });

  // Vercel Rule: rerender-move-effect-to-event — useSWRMutation for event-driven
  const { trigger: reviewReport, isMutating: isReviewing } =
    useReviewBlogReport();
  const { trigger: banBlog, isMutating: isBanning } = useBanBlog();

  const reportsList = useMemo(
    () => reportsRes?.items || reportsRes || [],
    [reportsRes],
  );

  // Vercel Rule: rerender-memo — useMemo for columns
  const columns = useMemo(
    () => [
      {
        key: "blogTitle",
        label: "Bài viết",
        sortable: true,
        render: (value) => (
          <p
            className="font-semibold text-zinc-900 max-w-[220px] truncate"
            title={value}
          >
            {value || "—"}
          </p>
        ),
      },
      {
        key: "reporterName",
        label: "Người báo cáo",
        render: (value, row) => (
          <div className="flex items-center gap-2">
            <Avatar className="h-7 w-7 border border-zinc-200">
              <AvatarImage src={row.reporterAvatar} />
              <AvatarFallback className="bg-blue-50 text-blue-600 text-[10px]">
                {(value || "U").charAt(0)}
              </AvatarFallback>
            </Avatar>
            <div className="flex flex-col">
              <span className="text-sm font-medium text-zinc-800">
                {value || "Ẩn danh"}
              </span>
              <span className="text-xs text-zinc-400">
                {row.reporterEmail || ""}
              </span>
            </div>
          </div>
        ),
      },
      {
        key: "reportType",
        label: "Loại",
        render: (value) => (
          <span className="text-xs font-medium px-2.5 py-1 bg-zinc-100 text-zinc-600 rounded-full">
            {REPORT_TYPE_LABELS[value] || value || "—"}
          </span>
        ),
      },
      {
        key: "reason",
        label: "Lý do",
        render: (value) => (
          <p
            className="text-sm text-zinc-600 max-w-[180px] truncate"
            title={value}
          >
            {value || "—"}
          </p>
        ),
      },
      {
        key: "status",
        label: "Trạng thái",
        render: (value) => <StatusBadge status={value} />,
      },
      {
        key: "createdDate",
        label: "Ngày báo cáo",
        sortable: true,
        render: (value) => {
          if (!value) return "—";
          const date = new Date(value);
          return (
            <span className="font-[family-name:var(--font-cms-mono)] text-sm text-zinc-600">
              {date.toLocaleDateString("vi-VN")}
            </span>
          );
        },
      },
    ],
    [],
  );

  // Vercel Rule: rerender-memo — filter data
  const filteredData = useMemo(() => {
    return Array.isArray(reportsList) ? reportsList : [];
  }, [reportsList]);

  const stats = useMemo(
    () => ({
      pending: Array.isArray(reportsList)
        ? reportsList.filter((r) => r.status === "PENDING").length
        : 0,
      total: Array.isArray(reportsList) ? reportsList.length : 0,
    }),
    [reportsList],
  );

  // Vercel Rule: rerender-functional-setstate — wrap with useCallback
  const handleViewReport = useCallback((report) => {
    setSelectedReport(report);
    setSheetOpen(true);
  }, []);

  const handleAcceptReport = useCallback(
    async (report) => {
      setReportToProcess(report);
      setBanDialogOpen(true);
    },
    [],
  );

  const handleConfirmBan = useCallback(async () => {
    if (!reportToProcess) return;
    try {
      // 1. Review report as APPROVED
      await reviewReport({
        reportId: reportToProcess.id,
        status: 2, // 2 = APPROVED
        adminNote: "Chấp nhận báo cáo vi phạm - Blog đã bị ẩn",
      });
      toast.success("Đã chấp nhận báo cáo. Blog sẽ bị ẩn.");
      mutate();
      setBanDialogOpen(false);
      setReportToProcess(null);
      setSheetOpen(false);
    } catch (e) {
      console.error(e);
      toast.error(e?.response?.data?.message || "Lỗi khi xử lý báo cáo");
    }
  }, [reportToProcess, reviewReport, mutate]);

  const handleRejectReport = useCallback(
    (report) => {
      setReportToProcess(report || selectedReport);
      setRejectDialogOpen(true);
    },
    [selectedReport],
  );

  const handleConfirmReject = useCallback(async () => {
    if (!reportToProcess) return;
    try {
      await reviewReport({
        reportId: reportToProcess.id,
        status: 3, // 3 = REJECTED
        adminNote: "Từ chối báo cáo - Nội dung không vi phạm",
      });
      toast.success("Đã từ chối báo cáo.");
      mutate();
      setRejectDialogOpen(false);
      setReportToProcess(null);
      setSheetOpen(false);
    } catch (e) {
      console.error(e);
      toast.error(e?.response?.data?.message || "Lỗi khi xử lý báo cáo");
    }
  }, [reportToProcess, reviewReport, mutate]);

  // Vercel Rule: rerender-functional-setstate — wrap with useCallback
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
        <DropdownMenuContent align="end" className="w-52">
          <DropdownMenuItem
            onClick={() => handleViewReport(row)}
            className="cursor-pointer"
          >
            <Eye className="h-4 w-4 mr-2" />
            Xem chi tiết
          </DropdownMenuItem>
          {row.status === "PENDING" && (
            <>
              <DropdownMenuSeparator className="bg-zinc-100" />
              <DropdownMenuItem
                onClick={() => handleAcceptReport(row)}
                className="text-emerald-600 focus:text-emerald-700 focus:bg-emerald-50 cursor-pointer"
              >
                <CheckCircle className="h-4 w-4 mr-2" />
                Chấp nhận (Ẩn blog)
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => handleRejectReport(row)}
                className="text-red-600 focus:text-red-700 focus:bg-red-50 cursor-pointer"
              >
                <XCircle className="h-4 w-4 mr-2" />
                Từ chối báo cáo
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    ),
    [handleViewReport, handleAcceptReport, handleRejectReport],
  );

  return (
    <div className="space-y-6">
      {/* Stats */}
      <div className="flex items-center gap-6 text-sm">
        <div className="flex items-center gap-2 text-amber-600">
          <AlertTriangle className="h-4 w-4" />
          <span className="font-medium">{stats.pending} chờ xử lý</span>
        </div>
        <div className="text-zinc-400">|</div>
        <div className="text-zinc-500">
          Tổng cộng {stats.total} báo cáo
        </div>
      </div>

      {/* Tab Filters */}
      <Tabs
        value={activeTab}
        onValueChange={setActiveTab}
        className="bg-transparent border-b border-zinc-200 pb-px"
      >
        <TabsList className="bg-transparent p-0 h-auto gap-6 pb-2 overflow-x-auto justify-start whitespace-nowrap hide-scrollbar">
          {[
            { value: "PENDING", label: "Chờ xử lý" },
            { value: "APPROVED", label: "Đã chấp nhận" },
            { value: "REJECTED", label: "Đã từ chối" },
            { value: "all", label: "Tất cả" },
          ].map((tab) => (
            <TabsTrigger
              key={tab.value}
              value={tab.value}
              className="data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 data-[state=active]:border-blue-500 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium"
            >
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      {/* Data Table */}
      <DataTable
        columns={columns}
        data={filteredData}
        loading={loading}
        searchPlaceholder="Tìm kiếm theo tên bài viết hoặc người báo cáo..."
        sortConfig={{ key: "createdDate", direction: "desc" }}
        actions={renderActions}
        pagination={{
          currentPage: 1,
          totalPages: 1,
          from: 1,
          to: filteredData.length,
          total: filteredData.length,
        }}
        emptyMessage="Không có báo cáo nào."
      />

      {/* Detail Sheet */}
      <DetailSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        title="Chi tiết báo cáo"
        description={`Blog: ${selectedReport?.blogTitle || "—"}`}
        className="sm:max-w-xl"
      >
        {selectedReport && sheetOpen ? (
          <div className="space-y-6 pb-6">
            {/* Blog Info */}
            <div className="p-4 bg-zinc-50 border border-zinc-100 rounded-xl">
              <h5 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-2">
                Bài viết bị báo cáo
              </h5>
              <p className="font-semibold text-zinc-900">
                {selectedReport.blogTitle}
              </p>
            </div>

            {/* Reporter Info */}
            <div className="p-4 bg-zinc-50 border border-zinc-100 rounded-xl">
              <h5 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-3">
                Người báo cáo
              </h5>
              <div className="flex items-center gap-3">
                <Avatar className="h-10 w-10 border border-zinc-200">
                  <AvatarImage src={selectedReport.reporterAvatar} />
                  <AvatarFallback className="bg-blue-50 text-blue-600">
                    {(selectedReport.reporterName || "U").charAt(0)}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-zinc-800">
                    {selectedReport.reporterName || "Ẩn danh"}
                  </p>
                  <p className="text-sm text-zinc-500">
                    {selectedReport.reporterEmail}
                  </p>
                </div>
              </div>
            </div>

            {/* Report Details */}
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <span className="text-xs font-medium text-zinc-500">Loại:</span>
                <span className="text-xs font-medium px-2.5 py-1 bg-red-50 text-red-600 rounded-full">
                  {REPORT_TYPE_LABELS[selectedReport.reportType] ||
                    selectedReport.reportType}
                </span>
                <StatusBadge status={selectedReport.status} />
              </div>

              <div className="p-4 bg-zinc-50 border border-zinc-100 rounded-xl">
                <h5 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-2">
                  Lý do
                </h5>
                <p className="text-sm text-zinc-700 leading-relaxed">
                  {selectedReport.reason || "Không có lý do"}
                </p>
              </div>

              {selectedReport.description && (
                <div className="p-4 bg-zinc-50 border border-zinc-100 rounded-xl">
                  <h5 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-2">
                    Mô tả chi tiết
                  </h5>
                  <p className="text-sm text-zinc-700 leading-relaxed">
                    {selectedReport.description}
                  </p>
                </div>
              )}

              {selectedReport.adminNote && (
                <div className="p-4 bg-emerald-50 border border-emerald-100 rounded-xl">
                  <h5 className="text-xs font-semibold uppercase tracking-wider text-emerald-600 mb-2">
                    Ghi chú quản trị
                  </h5>
                  <p className="text-sm text-emerald-800 leading-relaxed">
                    {selectedReport.adminNote}
                  </p>
                </div>
              )}
            </div>

            {/* Actions */}
            {selectedReport.status === "PENDING" && (
              <div className="flex gap-3 pt-4 border-t border-zinc-100">
                <Button
                  variant="outline"
                  onClick={() => handleRejectReport(selectedReport)}
                  className="flex-1 border-red-200 hover:bg-red-50 text-red-600 hover:text-red-700"
                  disabled={isReviewing}
                >
                  <XCircle className="h-4 w-4 mr-2" />
                  Từ chối
                </Button>
                <Button
                  onClick={() => handleAcceptReport(selectedReport)}
                  className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white"
                  disabled={isReviewing}
                >
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Chấp nhận (Ẩn blog)
                </Button>
              </div>
            )}
          </div>
        ) : null}
      </DetailSheet>

      {/* Accept Report + Ban Blog Confirmation */}
      <ConfirmDialog
        open={banDialogOpen}
        onOpenChange={setBanDialogOpen}
        title="Chấp nhận báo cáo vi phạm"
        description={`Bạn xác nhận chấp nhận báo cáo này? Blog "${reportToProcess?.blogTitle}" sẽ bị ẩn khỏi hệ thống.`}
        onConfirm={handleConfirmBan}
        loading={isReviewing || isBanning}
        isDestructive={true}
        confirmText="Chấp nhận & Ẩn blog"
        cancelText="Hủy bỏ"
      />

      {/* Reject Report Confirmation */}
      <ConfirmDialog
        open={rejectDialogOpen}
        onOpenChange={setRejectDialogOpen}
        title="Từ chối báo cáo"
        description={`Bạn xác nhận từ chối báo cáo này? Blog "${reportToProcess?.blogTitle}" sẽ không bị ảnh hưởng.`}
        onConfirm={handleConfirmReject}
        loading={isReviewing}
        confirmText="Từ chối báo cáo"
        cancelText="Hủy bỏ"
      />
    </div>
  );
};

export default BlogReportsTab;
