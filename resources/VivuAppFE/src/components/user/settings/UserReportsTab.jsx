import React, { useState } from "react";
import {
  Search,
  MapPin,
  Clock,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  FileX,
  Info,
  Pencil,
  ChevronRight,
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import {
  useMyLocationReports,
  useUpdateLocationReport,
} from "../../../hooks/locations/useLocationReports";
import toast from "../../../utils/toast";
import AddressAutocomplete from "../locations/AddressAutocomplete";

// Hoisted static constants
const STATUS_FILTERS = [
  { value: null, label: "Tất cả" },
  { value: "PENDING", label: "Đang xem xét" },
  { value: "APPROVED", label: "Đã xử lý" },
  { value: "REJECTED", label: "Từ chối" },
];

const REPORT_TYPE_LABELS = {
  WRONG_INFO: "Sai thông tin",
  CLOSED: "Đã đóng cửa",
  NEW_LOCATION: "Địa điểm mới",
  OTHER: "Khác",
};

const getStatusDetails = (status) => {
  const normalized = (status || "").toUpperCase();
  switch (normalized) {
    case "PENDING":
      return {
        icon: Clock,
        label: "Đang xem xét",
        className: "text-amber-600 bg-amber-50 border-amber-200",
      };
    case "APPROVED":
      return {
        icon: CheckCircle2,
        label: "Đã xử lý",
        className: "text-green-600 bg-green-50 border-green-200",
      };
    case "REJECTED":
      return {
        icon: XCircle,
        label: "Từ chối",
        className: "text-red-600 bg-red-50 border-red-200",
      };
    default:
      return {
        icon: Clock,
        label: "Không xác định",
        className: "text-gray-600 bg-gray-50 border-gray-200",
      };
  }
};

const emptyState = (
  <div className="flex flex-col items-center justify-center py-16 px-4 text-center">
    <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center text-gray-400 mb-6">
      <FileX size={32} />
    </div>
    <h3 className="text-xl font-bold text-gray-900 mb-2">
      Chưa có báo cáo nào
    </h3>
    <p className="text-gray-500 max-w-md">
      Bạn chưa báo cáo địa điểm nào hoặc không có báo cáo nào khớp với bộ lọc
      hiện tại.
    </p>
  </div>
);

const SkeletonList = () => (
  <div className="grid gap-4">
    {[1, 2, 3].map((i) => (
      <div
        key={`skeleton-${i}`}
        className="p-5 rounded-2xl border border-gray-100 bg-white"
      >
        <div className="flex justify-between items-start">
          <div className="space-y-3 w-2/3">
            <div className="h-5 bg-gray-200 rounded w-3/4 animate-pulse" />
            <div className="h-4 bg-gray-200 rounded w-1/2 animate-pulse" />
          </div>
          <div className="h-6 w-24 bg-gray-200 rounded-full animate-pulse" />
        </div>
      </div>
    ))}
  </div>
);

// Memoized report card
const ReportCard = React.memo(({ report, onEdit }) => {
  const {
    icon: StatusIcon,
    label: statusLabel,
    className: statusColor,
  } = getStatusDetails(report.status);

  const reportTypeLabel =
    REPORT_TYPE_LABELS[report.reportType] || report.reportType || "Khác";

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      onClick={() =>
        report.status?.toUpperCase() === "PENDING" && onEdit(report)
      }
      className={`group relative p-5 rounded-2xl border border-gray-100 bg-white hover:border-blue-100 hover:shadow-lg hover:shadow-blue-500/5 transition-all ${report.status?.toUpperCase() === "PENDING" ? "cursor-pointer" : ""}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
        <div className="space-y-2 flex-1">
          <h4 className="font-bold text-lg text-gray-900 group-hover:text-blue-600 transition-colors">
            {report.locationName || "Tên địa điểm"}
          </h4>
          <div className="flex items-start gap-2 text-sm text-gray-500">
            <MapPin size={16} className="mt-0.5 shrink-0" />
            <span className="line-clamp-2">
              {report.locationAddress || "Chưa có địa chỉ chi tiết"}
            </span>
          </div>

          {/* Report reason */}
          <div className="flex items-start gap-2 text-sm text-gray-600 mt-1">
            <AlertTriangle
              size={16}
              className="mt-0.5 shrink-0 text-orange-400"
            />
            <span className="line-clamp-2">
              {report.reason || "Không có lý do chi tiết"}
            </span>
          </div>

          {/* Metadata */}
          <div className="flex flex-wrap items-center gap-3 pt-2 text-xs text-gray-500 font-medium">
            <span className="px-2.5 py-1 rounded-lg bg-gray-100">
              {reportTypeLabel}
            </span>
            <span>•</span>
            <span>
              Gửi lúc:{" "}
              {new Date(
                report.createdDate || "2024-01-01T00:00:00Z",
              ).toLocaleDateString("vi-VN")}
            </span>
          </div>

          {/* Admin note */}
          {report.adminNote && (
            <div className="mt-3 flex items-start gap-2 p-3 rounded-xl bg-blue-50 border border-blue-100 text-sm">
              <Info size={16} className="mt-0.5 shrink-0 text-blue-500" />
              <div>
                <span className="font-semibold text-blue-700">
                  Phản hồi từ quản trị:{" "}
                </span>
                <span className="text-blue-600">{report.adminNote}</span>
              </div>
            </div>
          )}
        </div>

        <div className="flex items-center sm:items-end flex-row sm:flex-col justify-between sm:justify-start shrink-0">
          <div
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold border ${statusColor}`}
          >
            <StatusIcon size={14} />
            {statusLabel}
          </div>
          {report.status?.toUpperCase() === "PENDING" ? (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onEdit(report);
              }}
              className="mt-3 sm:mt-4 inline-flex h-8 w-8 items-center justify-center rounded-full text-blue-600 hover:bg-blue-50 transition-colors"
              title="Chỉnh sửa báo cáo"
            >
              <Pencil size={16} />
            </button>
          ) : (
            <div className="hidden sm:flex w-8 h-8 rounded-full items-center justify-center text-gray-400 group-hover:bg-blue-50 group-hover:text-blue-600 transition-colors mt-4">
              <ChevronRight size={18} />
            </div>
          )}
        </div>
      </div>
    </motion.div>
  );
});
ReportCard.displayName = "ReportCard";

const UserReportsTab = () => {
  const [statusFilter, setStatusFilter] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");

  const [editingReport, setEditingReport] = useState(null);
  const [editForm, setEditForm] = useState({
    reportType: "",
    reason: "",
    description: "",
    suggestedName: "",
    suggestedAddress: "",
  });
  const [editImages, setEditImages] = useState([]);

  const { data, isLoading, mutate } = useMyLocationReports({
    pageNumber: 1,
    pageSize: 50,
    ...(statusFilter ? { status: statusFilter } : {}),
  });

  const { trigger: updateReport, isMutating: isUpdatingReport } =
    useUpdateLocationReport();

  const reports = data?.items || [];

  // Client-side search
  const filteredReports = reports.filter(
    (r) =>
      !searchQuery ||
      (r.locationName &&
        r.locationName.toLowerCase().includes(searchQuery.toLowerCase())) ||
      (r.locationAddress &&
        r.locationAddress.toLowerCase().includes(searchQuery.toLowerCase())),
  );

  const openEditModal = (report) => {
    setEditingReport(report);
    setEditForm({
      reportType: report.reportType || "OTHER",
      reason: report.reason || "",
      description: report.description || "",
      suggestedName: report.suggestedName || "",
      suggestedAddress: report.suggestedAddress || "",
    });
    setEditImages([]);
  };

  const closeEditModal = () => {
    setEditingReport(null);
    setEditImages([]);
  };

  const handleEditChange = (field, value) => {
    setEditForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleImageChange = (event) => {
    setEditImages(Array.from(event.target.files || []));
  };

  const submitEdit = async () => {
    if (!editingReport) return;
    if (!editForm.reason.trim()) {
      toast.error("Vui lòng nhập lý do báo cáo.");
      return;
    }

    const fd = new FormData();
    fd.append("ReportType", editForm.reportType);
    fd.append("ReportReason", editForm.reason.trim());
    if (editForm.description)
      fd.append("ReportDescription", editForm.description);
    if (editForm.suggestedName)
      fd.append("SuggestedName", editForm.suggestedName);
    if (editForm.suggestedAddress)
      fd.append("SuggestedAddress", editForm.suggestedAddress);

    editImages.forEach((file) => fd.append("EvidenceImages", file));

    try {
      await updateReport({ id: editingReport.id, formData: fd });
      toast.success("Cập nhật báo cáo thành công.");
      await mutate();
      closeEditModal();
    } catch (error) {
      toast.error(
        error?.response?.data?.message || "Cập nhật báo cáo thất bại.",
      );
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row gap-4 justify-between">
        <div className="flex gap-1.5 bg-gray-100/80 p-1.5 rounded-xl self-start overflow-x-auto hide-scrollbar w-full sm:w-auto">
          {STATUS_FILTERS.map((f) => (
            <button
              key={f.value ?? "all"}
              onClick={() => setStatusFilter(f.value)}
              className={`px-4 py-2 rounded-lg text-sm font-semibold transition-all whitespace-nowrap ${
                statusFilter === f.value
                  ? "bg-white text-gray-900 shadow-sm"
                  : "text-gray-500 hover:text-gray-700 hover:bg-black/5"
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>

        <div className="relative w-full sm:w-64">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 w-4 h-4" />
          <input
            type="text"
            placeholder="Tìm địa điểm..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2.5 bg-white border border-gray-200 rounded-xl text-sm focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all"
          />
        </div>
      </div>

      <AnimatePresence mode="wait">
        {isLoading ? (
          <motion.div
            key="loading"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            <SkeletonList />
          </motion.div>
        ) : filteredReports.length > 0 ? (
          <motion.div
            key="list"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="grid gap-4"
          >
            {filteredReports.map((r) => (
              <ReportCard key={r.id} report={r} onEdit={openEditModal} />
            ))}
          </motion.div>
        ) : (
          <motion.div
            key="empty"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            {emptyState}
          </motion.div>
        )}
      </AnimatePresence>

      {editingReport && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-xl rounded-2xl bg-white p-5 shadow-xl max-h-[90vh] overflow-y-auto space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                Chỉnh sửa báo cáo địa điểm
              </h3>
              <button
                type="button"
                onClick={closeEditModal}
                className="text-gray-500 hover:text-gray-700 transition"
              >
                <XCircle size={20} />
              </button>
            </div>

            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Loại báo cáo
                </label>
                <select
                  value={editForm.reportType}
                  onChange={(e) =>
                    handleEditChange("reportType", e.target.value)
                  }
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                >
                  {Object.entries(REPORT_TYPE_LABELS).map(([val, label]) => (
                    <option key={val} value={val}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Lý do <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={editForm.reason}
                  onChange={(e) => handleEditChange("reason", e.target.value)}
                  placeholder="Vui lòng nhập lý do cụ thể"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Tên đề xuất
                </label>
                <input
                  type="text"
                  value={editForm.suggestedName}
                  onChange={(e) =>
                    handleEditChange("suggestedName", e.target.value)
                  }
                  placeholder="Nếu tên địa điểm sai, nhập tên đúng"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <AddressAutocomplete
                value={editForm.suggestedAddress}
                onChange={(val) => handleEditChange("suggestedAddress", val)}
                onSelectPlace={(address) =>
                  handleEditChange("suggestedAddress", address)
                }
              />

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Mô tả thêm
                </label>
                <textarea
                  value={editForm.description}
                  onChange={(e) =>
                    handleEditChange("description", e.target.value)
                  }
                  placeholder="Mô tả chi tiết hơn về vấn đề..."
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm min-h-24 outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Ảnh Bằng Chứng (Tùy chọn)
                </label>
                <input
                  type="file"
                  multiple
                  accept="image/*"
                  onChange={handleImageChange}
                  className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm text-gray-700
                    file:mr-4 file:py-2 file:px-4
                    file:rounded-lg file:border-0
                    file:text-sm file:font-semibold
                    file:bg-blue-50 file:text-blue-700
                    hover:file:bg-blue-100"
                />
                <p className="text-xs text-gray-500 mt-1">
                  Chỉ upload ảnh mới nếu bạn muốn thay đổi toàn bộ ảnh hiện tại.
                  Nếu để trống, ảnh cũ sẽ được giữ nguyên.
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-4">
              <button
                type="button"
                onClick={closeEditModal}
                className="rounded-lg border border-gray-200 px-4 py-2 bg-white text-sm font-semibold text-gray-700 hover:bg-gray-50 transition"
              >
                Hủy
              </button>
              <button
                type="button"
                disabled={isUpdatingReport}
                onClick={submitEdit}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 transition disabled:opacity-60"
              >
                {isUpdatingReport ? "Đang cập nhật..." : "Cập nhật báo cáo"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserReportsTab;
