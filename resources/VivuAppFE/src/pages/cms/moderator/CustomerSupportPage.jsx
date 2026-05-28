import React, { useState, useMemo, useCallback } from "react";
import {
  Eye,
  MoreHorizontal,
  MessageCircle,
  CheckCircle,
  Clock,
  AlertTriangle,
  Send,
  User as UserIcon,
  Bot
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Skeleton } from "@/components/ui/skeleton";
import DataTable from "@/components/cms/common/DataTable";
import DetailSheet from "@/components/cms/common/DetailSheet";
import StatusBadge from "@/components/cms/common/StatusBadge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  useLocationReports,
  useReviewReport,
} from "@/hooks/locations/useLocationReports";
import useDebounce from "@/hooks/utils/useDebounce";
import toast from "@/utils/toast";
import { cn } from "@/lib/utils";
import { useLocation } from "react-router-dom";

const CustomerSupportPage = () => {
  const [activeTab, setActiveTab] = useState("all");
  const [priorityFilter, setPriorityFilter] = useState("all");
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 300);
  const [sortConfig, setSortConfig] = useState({
    key: "createdAt",
    direction: "desc",
  });
  const [replyText, setReplyText] = useState("");

  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  const tabBorderClass = isAdminTheme ? "data-[state=active]:border-emerald-400" : "data-[state=active]:border-blue-400";
  const activePillClass = isAdminTheme ? "bg-emerald-400 text-white shadow-md shadow-emerald-400/20 hover:bg-emerald-500" : "bg-blue-400 text-white shadow-md shadow-blue-400/20 hover:bg-blue-500";

  const {
    data: reportsRes,
    isLoading: loading,
    mutate,
  } = useLocationReports({
    pageNumber: 1,
    pageSize: 100,
  });
  const { trigger: reviewReport, isMutating } = useReviewReport();

  const reportsList = useMemo(
    () => reportsRes?.items || reportsRes || [],
    [reportsRes],
  );

  // ✅ Vercel rule: rerender-memo — Wrap columns trong useMemo
  // ✅ BUG 5 FIX: Sửa key customer→reporterName, priority→reportType theo LocationReportDto
  const columns = useMemo(() => [
    {
      key: "id",
      label: "Mã ticket",
      sortable: true,
      render: (value) => (
        <span className="font-[family-name:var(--font-cms-mono)] text-sm font-semibold text-zinc-600">#{value}</span>
      ),
    },
    {
      key: "reason",
      label: "Lý do báo cáo",
      sortable: true,
      render: (value) => (
        <p className="font-medium text-zinc-900 max-w-[250px] truncate" title={value || "Không có lý do"}>
          {value || "Không có lý do"}
        </p>
      ),
    },
    {
      key: "reporterName",
      label: "Người báo cáo",
      render: (value, row) => (
        <div className="flex items-center gap-3">
          <Avatar className="h-8 w-8 border border-zinc-200 shadow-sm">
            <AvatarImage src={row?.reporterAvatar} />
            <AvatarFallback className="bg-blue-50 text-blue-600 font-medium text-xs">
              {(row?.reporterName || "U").charAt(0)}
            </AvatarFallback>
          </Avatar>
          <div className="flex flex-col">
            <span className="text-sm font-medium text-zinc-900">{row?.reporterName || "Ẩn danh"}</span>
            <span className="text-xs text-zinc-500">{row?.reporterEmail || ""}</span>
          </div>
        </div>
      ),
    },
    {
      key: "reportType",
      label: "Loại báo cáo",
      sortable: true,
      render: (value) => <StatusBadge status={value || "OTHER"} />,
    },
    {
      key: "status",
      label: "Trạng thái",
      sortable: true,
      render: (value) => <StatusBadge status={value} />,
    },
    {
      key: "createdDate",
      label: "Thời gian",
      sortable: true,
      render: (value) => {
        if (!value) return "-";
        const date = new Date(value);
        return (
          <div className="flex flex-col text-sm font-[family-name:var(--font-cms-mono)] text-zinc-600">
            <span>{date.toLocaleDateString("vi-VN")}</span>
            <span className="text-xs text-zinc-400">
              {date.toLocaleTimeString("vi-VN", {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
          </div>
        );
      },
    },
  ], []);

  // ✅ BUG 5 FIX: Cập nhật filter/search dùng đúng field API (reporterName, reportType)
  const filteredData = useMemo(() => {
    let result = Array.isArray(reportsList) ? [...reportsList] : [];

    if (activeTab !== "all") {
      result = result.filter((ticket) => ticket.status === activeTab);
    }
    if (priorityFilter !== "all") {
      result = result.filter((ticket) => ticket.reportType === priorityFilter);
    }
    if (debouncedSearch) {
      const q = debouncedSearch.toLowerCase();
      result = result.filter(
        (ticket) =>
          ticket.id?.toString().toLowerCase().includes(q) ||
          ticket.reason?.toLowerCase().includes(q) ||
          ticket.reporterName?.toLowerCase().includes(q) ||
          ticket.reporterEmail?.toLowerCase().includes(q) ||
          ticket.locationName?.toLowerCase().includes(q),
      );
    }

    if (sortConfig) {
      result.sort((a, b) => {
        const aVal = a[sortConfig.key];
        const bVal = b[sortConfig.key];
        if (!aVal && bVal) return sortConfig.direction === "asc" ? -1 : 1;
        if (aVal && !bVal) return sortConfig.direction === "asc" ? 1 : -1;
        if (sortConfig.direction === "asc") {
          return aVal > bVal ? 1 : -1;
        }
        return aVal < bVal ? 1 : -1;
      });
    }

    return result;
  }, [activeTab, priorityFilter, debouncedSearch, sortConfig, reportsList]);

  const handleSort = useCallback((key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  }, []);

  const handleViewTicket = useCallback((ticket) => {
    setSelectedTicket(ticket);
    setSheetOpen(true);
    setReplyText("");
  }, []);

  // ✅ BUG 6 FIX: Tách handleReply thành handleAddNote (ghi chú) + handleDecision (duyệt/từ chối)
  // ✅ Vercel rule: rerender-functional-setstate — wrap useCallback
  const handleAddNote = useCallback(async () => {
    if (!replyText.trim() || !selectedTicket) return;
    try {
      await reviewReport({
        reportId: selectedTicket.id,
        decision: selectedTicket.status || "PENDING",
        adminNote: replyText,
      });
      toast.success("Đã ghi chú nội bộ");
      setReplyText("");
      mutate();
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi ghi chú");
    }
  }, [replyText, selectedTicket, reviewReport, mutate]);

  const handleDecision = useCallback(async (decision) => {
    if (!selectedTicket) return;
    try {
      await reviewReport({
        reportId: selectedTicket.id,
        decision,
        adminNote: replyText || `${decision} via CMS`,
      });
      toast.success(decision === "APPROVED" ? "Đã chấp nhận báo cáo" : "Đã từ chối báo cáo");
      setReplyText("");
      mutate();
      setSheetOpen(false);
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi xử lý báo cáo");
    }
  }, [selectedTicket, replyText, reviewReport, mutate]);

  const renderActions = useCallback((row) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="group-hover:bg-zinc-200/50 transition-colors">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48 shadow-[var(--shadow-cms-card-hover)] border-zinc-100 rounded-xl">
        <DropdownMenuItem onClick={() => handleViewTicket(row)} className="cursor-pointer">
          <Eye className="h-4 w-4 mr-2" />
          Mở Dashboard Hỗ trợ
        </DropdownMenuItem>
        {row.status !== "resolved" && row.status !== "closed" && (
          <DropdownMenuItem
            onClick={() => handleDecision("APPROVED")}
            className="text-emerald-600 focus:bg-emerald-50 focus:text-emerald-700 cursor-pointer"
          >
            <CheckCircle className="h-4 w-4 mr-2" />
            Chấp nhận báo cáo
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  ), [handleViewTicket, handleDecision]);

  // ✅ BUG 5 FIX: Stats dùng đúng field API (reportType thay priority)
  const stats = useMemo(() => ({
    open: reportsList.filter(
      (t) => t.status === "open" || t.status === "pending" || t.status === "PENDING" || t.status === "draft",
    ).length,
    inProgress: reportsList.filter(
      (t) => t.status === "in-progress" || t.status === "reviewing" || t.status === "REVIEWING"
    ).length,
    reportCount: reportsList.filter(
      (t) => t.status !== "resolved" && t.status !== "APPROVED" && t.status !== "REJECTED",
    ).length,
  }), [reportsList]);

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-6">
        {/* Header */}
        <div className="border-b border-zinc-200 pb-6">
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">Hỗ trợ Khách hàng</h1>
          <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
            Xem xét và phản hồi các báo cáo, yêu cầu hỗ trợ (tickets) từ bộ phận CSKH
          </p>
        </div>

        {/* Inline Stats */}
        <div>
          <div className="flex flex-col sm:flex-row items-center divide-y sm:divide-y-0 sm:divide-x divide-zinc-200 bg-[var(--color-cms-card)] border border-zinc-200 rounded-xl overflow-hidden shadow-[var(--shadow-cms-card)]">
            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-amber-50 flex items-center justify-center text-amber-600">
                <Clock className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">Đang mở</p>
                {loading ? <Skeleton className="h-8 w-16 mt-1.5" /> : <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">{stats.open}</h3>}
              </div>
            </div>
            
            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-blue-600">
                <MessageCircle className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">Đang xử lý</p>
                {loading ? <Skeleton className="h-8 w-16 mt-1.5" /> : <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">{stats.inProgress}</h3>}
              </div>
            </div>

            <div className="flex items-center gap-4 p-5 flex-1 w-full hover:bg-zinc-50/50 transition-colors">
              <div className="w-12 h-12 rounded-xl bg-red-50 flex items-center justify-center text-red-600">
                <AlertTriangle className="h-6 w-6" />
              </div>
              <div>
                <p className="text-xs font-medium text-zinc-500 uppercase tracking-wider">Chưa xử lý</p>
                {loading ? <Skeleton className="h-8 w-16 mt-1.5" /> : <h3 className="text-2xl font-semibold mt-1 text-zinc-900 font-[family-name:var(--font-cms-mono)] tabular-nums">{stats.reportCount}</h3>}
              </div>
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4 py-2">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="bg-transparent border-b border-zinc-200 pb-px w-full lg:w-auto">
            <TabsList className="bg-transparent p-0 h-auto gap-6 pb-2 overflow-x-auto w-full justify-start whitespace-nowrap hide-scrollbar">
              <TabsTrigger 
                value="all" 
                className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 ${tabBorderClass} rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium`}
              >
                Tất cả
              </TabsTrigger>
              <TabsTrigger 
                value="pending" 
                className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 ${tabBorderClass} rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium`}
              >
                Đang mở
              </TabsTrigger>
              <TabsTrigger 
                value="resolved" 
                className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 ${tabBorderClass} rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium`}
              >
                Đã giải quyết
              </TabsTrigger>
            </TabsList>
          </Tabs>

          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium text-zinc-500 mr-2">Loại:</span>
            {["all", "WRONG_INFO", "CLOSED", "DUPLICATE", "OTHER"].map((type) => (
              <Button
                key={type}
                variant={priorityFilter === type ? "default" : "outline"}
                size="sm"
                onClick={() => setPriorityFilter(type)}
                className={cn(
                   "cms-btn-interactive rounded-full text-xs h-8 px-4",
                   priorityFilter === type 
                     ? activePillClass 
                     : "bg-white text-zinc-600 hover:bg-zinc-100 border border-zinc-200"
                )}
              >
                {type === "all" ? "Tất cả" : type === "WRONG_INFO" ? "Sai thông tin" : type === "CLOSED" ? "Đã đóng" : type === "DUPLICATE" ? "Trùng lặp" : "Khác"}
              </Button>
            ))}
          </div>
        </div>

        {/* Data Table */}
        <div>
          <DataTable
            columns={columns}
            data={filteredData}
            loading={loading}
            searchPlaceholder="Tìm kiếm ID, tên khách hàng hoặc tiêu đề..."
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
            emptyMessage="Tuyệt vời! Không có ticket nào đang tồn đọng."
          />
        </div>

        {/* Detail Sheet - Chat UI Redesign */}
        <DetailSheet
          open={sheetOpen}
          onOpenChange={setSheetOpen}
          title={`Ticket #${selectedTicket?.id}`}
          description={selectedTicket?.reason || selectedTicket?.subject || "Không có tiêu đề"}
          className="sm:max-w-xl"
        >
          {selectedTicket && sheetOpen ? (
            <div className="flex flex-col h-[calc(100vh-160px)] -mt-2">
              
              {/* Customer Banner */}
              {/* ✅ BUG 5 FIX: Dùng đúng field API (reporterName, reporterEmail, reporterAvatar, reportType) */}
              <div className="flex items-center gap-4 p-4 bg-zinc-50 border border-zinc-100 rounded-xl mb-4 shrink-0">
                <Avatar className="h-12 w-12 border border-zinc-200">
                  <AvatarImage src={selectedTicket.reporterAvatar} />
                  <AvatarFallback className="bg-blue-50 text-blue-600 font-medium">
                    {(selectedTicket.reporterName || "U").charAt(0)}
                  </AvatarFallback>
                </Avatar>
                <div className="flex-1">
                  <p className="font-semibold text-zinc-900">
                    {selectedTicket.reporterName || "Ẩn danh"}
                  </p>
                  <p className="text-sm text-zinc-500 font-medium">
                    {selectedTicket.reporterEmail || ""}
                  </p>
                </div>
                <div className="flex flex-col items-end gap-1.5">
                  <StatusBadge status={selectedTicket.reportType || "OTHER"} />
                  <StatusBadge status={selectedTicket.status} />
                </div>
              </div>

              {/* Chat View Scrollable */}
              <div className="flex-1 overflow-y-auto hide-scrollbar space-y-4 px-1 rounded-md">
                
                {selectedTicket.reason && (
                  <div className="flex flex-col items-start gap-1">
                     <span className="text-[11px] font-medium text-zinc-400 uppercase tracking-wider ml-12">Yêu cầu ban đầu</span>
                     <div className="flex items-start gap-3 w-3/4">
                       <Avatar className="h-8 w-8 shrink-0 mt-1 border border-zinc-200">
                          <AvatarFallback className="bg-blue-50 text-blue-600 text-xs"><UserIcon className="h-4 w-4" /></AvatarFallback>
                       </Avatar>
                       <div className="bg-zinc-100 p-3.5 rounded-2xl rounded-tl-sm text-sm text-zinc-800 leading-relaxed">
                         {selectedTicket.reason}
                       </div>
                     </div>
                  </div>
                )}
                
                {selectedTicket.messages?.map((msg, index) => {
                  const isCustomer = msg.from === "customer";
                  return (
                    <div key={index} className={cn("flex flex-col gap-1", isCustomer ? "items-start" : "items-end")}>
                       <span className={cn("text-[11px] font-medium text-zinc-400 uppercase tracking-wider mx-12")}>
                          {isCustomer ? "Khách hàng" : "Quản trị viên"}
                       </span>
                       <div className={cn("flex items-start gap-3 w-3/4", isCustomer ? "flex-row" : "flex-row-reverse")}>
                          <Avatar className="h-8 w-8 shrink-0 mt-1 border border-zinc-200">
                             <AvatarFallback className={cn("text-xs", isCustomer ? "bg-blue-50 text-blue-600" : "bg-emerald-50 text-emerald-600")}>
                                {isCustomer ? <UserIcon className="h-4 w-4" /> : <Bot className="h-4 w-4" />}
                             </AvatarFallback>
                          </Avatar>
                          <div className={cn(
                             "p-3.5 rounded-2xl text-sm leading-relaxed", 
                             isCustomer 
                               ? "bg-zinc-100/80 text-zinc-800 rounded-tl-sm" 
                               : "bg-emerald-50 text-emerald-900 border border-emerald-100/50 rounded-tr-sm"
                          )}>
                             {msg.text}
                          </div>
                       </div>
                    </div>
                  );
                })}
              </div>

              {/* Chat Input Area */}
              {/* ✅ BUG 6 FIX: Tách nút ghi chú / chấp nhận / từ chối */}
              {selectedTicket.status !== "APPROVED" && selectedTicket.status !== "REJECTED" ? (
                  <div className="pt-4 border-t border-zinc-100 mt-4 shrink-0 bg-white">
                    <div className="relative">
                      <Input
                        placeholder="Nhập ghi chú nội bộ..."
                        value={replyText}
                        onChange={(e) => setReplyText(e.target.value)}
                        className="pr-12 py-6 rounded-xl border-zinc-200 focus-visible:ring-emerald-500/20 focus-visible:border-emerald-500 shadow-sm"
                        onKeyDown={(e) => e.key === 'Enter' && handleAddNote()}
                      />
                      <Button 
                         size="icon" 
                         className="absolute right-2 top-2 h-8 w-8 bg-zinc-600 hover:bg-zinc-700 text-white rounded-lg cms-btn-interactive"
                         onClick={handleAddNote}
                         disabled={isMutating || !replyText.trim()}
                         title="Ghi chú nội bộ (không thay đổi trạng thái)"
                      >
                         <Send className="h-4 w-4" />
                      </Button>
                    </div>
                    
                    <div className="flex justify-between items-center mt-3 px-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDecision("REJECTED")}
                        className="text-red-600 hover:bg-red-50 hover:text-red-700 cms-btn-interactive h-8"
                        disabled={isMutating}
                      >
                        Từ chối
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDecision("APPROVED")}
                        className="text-emerald-600 hover:bg-emerald-50 hover:text-emerald-700 cms-btn-interactive h-8"
                        disabled={isMutating}
                      >
                        <CheckCircle className="h-4 w-4 mr-1.5" />
                        Chấp nhận báo cáo
                      </Button>
                    </div>
                  </div>
              ) : null}
            </div>
          ) : null}
        </DetailSheet>
      </div>
    </div>
  );
};

export default CustomerSupportPage;
