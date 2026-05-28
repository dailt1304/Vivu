import React, { useState } from "react";
import {
  Search,
  MapPin,
  Clock,
  CheckCircle2,
  XCircle,
  ChevronRight,
  FileX,
  Pencil,
} from "lucide-react";
// eslint-disable-next-line no-unused-vars
import { AnimatePresence, motion } from "framer-motion";
import toast from "../../../utils/toast";
import {
  useMySubmissions,
  useSuggestUpdateLocation,
} from "../../../hooks/locations/useLocations";
import WeeklyScheduleInput from "../../../components/common/inputs/WeeklyScheduleInput";
import AddressAutocomplete from "../locations/AddressAutocomplete";

// Vercel Rule: rendering-hoist-jsx - Hoist static constants
const STATUS_FILTERS = [
  { value: null, label: "Tất cả" },
  { value: 1, label: "Đang chờ", color: "text-amber-500 bg-amber-50" },
  { value: 2, label: "Đã duyệt", color: "text-green-500 bg-green-50" },
  { value: 3, label: "Từ chối", color: "text-red-500 bg-red-50" },
];

const normalizeSubmissionStatus = (submission, statusFilter = null) => {
  const raw = submission?.status ?? submission?.reportStatus ?? submission?.submissionStatus;

  if (typeof raw === "number") {
    if (raw === 1) return "PENDING";
    if (raw === 2) return "APPROVED";
    if (raw === 3) return "REJECTED";
    return "UNKNOWN";
  }

  const normalized = String(raw || "")
    .trim()
    .toUpperCase();

  if (normalized === "1" || normalized === "PENDING") return "PENDING";
  if (normalized === "2" || normalized === "APPROVED") return "APPROVED";
  if (normalized === "3" || normalized === "REJECTED") return "REJECTED";

  if (statusFilter === 1) return "PENDING";
  if (statusFilter === 2) return "APPROVED";
  if (statusFilter === 3) return "REJECTED";

  return "UNKNOWN";
};

// Helper to get status UI details outside component
const getStatusDetails = (submission, statusFilter = null) => {
  switch (normalizeSubmissionStatus(submission, statusFilter)) {
    case "PENDING":
      return {
        icon: Clock,
        label: "Đang chờ duyệt",
        className: "text-amber-600 bg-amber-50 border-amber-200",
      };
    case "APPROVED":
      return {
        icon: CheckCircle2,
        label: "Đã duyệt",
        className: "text-green-600 bg-green-50 border-green-200",
      };
    case "REJECTED":
      return {
        icon: XCircle,
        label: "Bị từ chối",
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

// Vercel Rule: rendering-hoist-jsx - Empty state JSX
const emptyState = (
  <div className="flex flex-col items-center justify-center py-16 px-4 text-center">
    <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center text-gray-400 mb-6">
      <FileX size={32} />
    </div>
    <h3 className="text-xl font-bold text-gray-900 mb-2">
      Chưa có địa điểm nào
    </h3>
    <p className="text-gray-500 max-w-md">
      Bạn chưa gửi địa điểm nào hoặc không có địa điểm nào khớp với bộ lọc hiện
      tại. Hãy chia sẻ những địa điểm thú vị mà bạn biết nhé!
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

// Vercel Rule: rerender-memo - Memoize list item
const SubmissionCard = React.memo(({ submission, canEdit, onEdit, statusFilter }) => {
  const {
    icon: StatusIcon,
    label: statusLabel,
    className: statusColor,
  } = getStatusDetails(submission, statusFilter);

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      onClick={() => canEdit && onEdit(submission)}
      className={`group relative p-5 rounded-2xl border border-gray-100 bg-white hover:border-blue-100 hover:shadow-lg hover:shadow-blue-500/5 transition-all outline-none ${canEdit ? "cursor-pointer" : ""}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
        <div className="space-y-2 flex-1 relative z-10">
          <h4 className="font-bold text-lg text-gray-900 group-hover:text-blue-600 transition-colors">
            {submission.name || "Tên địa điểm"}
          </h4>
          <div className="flex items-start gap-2 text-sm text-gray-500">
            <MapPin size={16} className="mt-0.5 shrink-0" />
            <span className="line-clamp-2">
              {submission.address || "Chưa có địa chỉ chi tiết"}
            </span>
          </div>

          {/* Metadata */}
          <div className="flex flex-wrap items-center gap-3 pt-2 text-xs text-gray-500 font-medium">
            <span className="px-2.5 py-1 rounded-lg bg-gray-100">
              {submission.categoryName || "Danh mục khác"}
            </span>
            <span>•</span>
            <span>
              Gửi lúc:{" "}
              {new Date(
                submission.createdDate || submission.createdAt || "2024-01-01T00:00:00Z",
              ).toLocaleDateString("vi-VN")}
            </span>
          </div>
        </div>

        <div className="flex items-center sm:items-end flex-row sm:flex-col justify-between sm:justify-start">
          <div
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-bold border ${statusColor}`}
          >
            <StatusIcon size={14} />
            {statusLabel}
          </div>
          {canEdit ? (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onEdit(submission);
              }}
              className="mt-3 sm:mt-4 inline-flex h-8 w-8 items-center justify-center rounded-full text-blue-600 hover:bg-blue-50 transition-colors shrink-0"
              title="Chỉnh sửa đề xuất"
              aria-label="Chỉnh sửa đề xuất"
            >
              <Pencil size={16} />
            </button>
          ) : (
            <div className="hidden sm:flex w-8 h-8 rounded-full items-center justify-center text-gray-400 group-hover:bg-blue-50 group-hover:text-blue-600 transition-colors mt-4 shrink-0">
              <ChevronRight size={18} />
            </div>
          )}
        </div>
      </div>
    </motion.div>
  );
});
SubmissionCard.displayName = "SubmissionCard";

const UserSubmissionsTab = () => {
  const [statusFilter, setStatusFilter] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [editingSubmission, setEditingSubmission] = useState(null);
  const [editForm, setEditForm] = useState({
    name: "",
    description: "",
    address: "",
    latitude: "",
    longitude: "",
    cityId: "",
    categoryId: "",
    openingHours: "",
    phone: "",
    website: "",
    tags: "",
  });
  const [editImages, setEditImages] = useState([]);

  // Vercel Rule: client-swr-dedup - SWR hook for automatic dedup and caching
  const { data, isLoading, mutate } = useMySubmissions({
    pageNumber: 1,
    pageSize: 50,
    ...(statusFilter ? { status: statusFilter } : {}),
  });
  const { trigger: suggestUpdate, isMutating: isUpdatingSuggestion } =
    useSuggestUpdateLocation();

  const submissions = data?.items || [];

  // Client-side search as backend lacks it
  const filteredSubmissions = submissions.filter(
    (s) =>
      !searchQuery ||
      (s.name && s.name.toLowerCase().includes(searchQuery.toLowerCase())) ||
      (s.address &&
        s.address.toLowerCase().includes(searchQuery.toLowerCase())),
  );

  const openEditModal = (submission) => {
    const details = submission.locationDetail || {};
    setEditingSubmission(submission);
    setEditForm({
      name: submission.name || "",
      description: submission.description || "",
      address: submission.address || "",
      latitude:
        submission.latitude != null ? String(submission.latitude) : "",
      longitude:
        submission.longitude != null ? String(submission.longitude) : "",
      cityId: submission.cityId || "",
      categoryId: submission.categoryId || "",
      openingHours: details.openingHours || "",
      phone: details.phone || "",
      website: details.website || "",
      tags: details.tags || "",
    });
    setEditImages([]);
  };

  const closeEditModal = () => {
    setEditingSubmission(null);
    setEditImages([]);
  };

  const handleEditChange = (field, value) => {
    setEditForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleImageChange = (event) => {
    setEditImages(Array.from(event.target.files || []));
  };

  const submitEditSuggestion = async () => {
    if (!editingSubmission) return;

    if (!editForm.name.trim() || !editForm.address.trim()) {
      toast.error("Vui lòng nhập tên và địa chỉ địa điểm.");
      return;
    }

    const fd = new FormData();
    const appendIfPresent = (key, value) => {
      if (value !== null && value !== undefined && value !== "") {
        fd.append(key, value);
      }
    };

    appendIfPresent("Name", editForm.name.trim());
    appendIfPresent("Description", editForm.description);
    appendIfPresent("Address", editForm.address.trim());
    appendIfPresent("Latitude", editForm.latitude);
    appendIfPresent("Longitude", editForm.longitude);
    appendIfPresent("CityId", editForm.cityId);
    appendIfPresent("CategoryId", editForm.categoryId);
    appendIfPresent("OpeningHours", editForm.openingHours);
    appendIfPresent("Phone", editForm.phone);
    appendIfPresent("Website", editForm.website);
    appendIfPresent("Tags", editForm.tags);

    editImages.forEach((file) => fd.append("Images", file));

    try {
      await suggestUpdate({
        id: editingSubmission.id,
        formData: fd,
      });
      toast.success("Đã gửi đề xuất chỉnh sửa địa điểm.");
      await mutate();
      closeEditModal();
    } catch (error) {
      const message =
        error?.response?.data?.message || "Gửi đề xuất chỉnh sửa thất bại.";
      toast.error(message);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row gap-4 justify-between">
        {/* Vercel Rule: rerender-functional-setstate - using functional setState inside wrapper or direct */}
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

      {/* Vercel Rule: rendering-conditional-render - Ternary operator for rendering states */}
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
        ) : filteredSubmissions.length > 0 ? (
          <motion.div
            key="list"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="grid gap-4"
          >
            {filteredSubmissions.map((s) => (
              <SubmissionCard
                key={s.id}
                submission={s}
                canEdit={normalizeSubmissionStatus(s, statusFilter) === "PENDING"}
                onEdit={openEditModal}
                statusFilter={statusFilter}
              />
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

      {editingSubmission && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-xl rounded-2xl bg-white p-5 shadow-xl max-h-[90vh] overflow-y-auto space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-900">
                Chỉnh sửa đề xuất địa điểm
              </h3>
              <button
                type="button"
                onClick={closeEditModal}
                className="text-gray-500 hover:text-gray-700"
              >
                <XCircle size={20} />
              </button>
            </div>

            <div className="grid gap-3">
              <input
                type="text"
                value={editForm.name}
                onChange={(e) => handleEditChange("name", e.target.value)}
                placeholder="Tên địa điểm"
                className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
              />
              <textarea
                value={editForm.description}
                onChange={(e) =>
                  handleEditChange("description", e.target.value)
                }
                placeholder="Mô tả"
                className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm min-h-24"
              />
              <AddressAutocomplete
                value={editForm.address}
                onChange={(val) => handleEditChange("address", val)}
                onSelectPlace={(address, lat, lng) => {
                  handleEditChange("address", address);
                  if (lat && lng) {
                    handleEditChange("latitude", String(lat));
                    handleEditChange("longitude", String(lng));
                  }
                }}
              />

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  type="number"
                  value={editForm.latitude}
                  onChange={(e) =>
                    handleEditChange("latitude", e.target.value)
                  }
                  placeholder="Latitude"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
                />
                <input
                  type="number"
                  value={editForm.longitude}
                  onChange={(e) =>
                    handleEditChange("longitude", e.target.value)
                  }
                  placeholder="Longitude"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
                />
              </div>

              <WeeklyScheduleInput
                value={editForm.openingHours}
                onChange={(json) => handleEditChange("openingHours", json)}
                compact={true}
                variant="user"
              />
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  type="text"
                  value={editForm.phone}
                  onChange={(e) => handleEditChange("phone", e.target.value)}
                  placeholder="Số điện thoại"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
                />
                <input
                  type="text"
                  value={editForm.website}
                  onChange={(e) =>
                    handleEditChange("website", e.target.value)
                  }
                  placeholder="Website"
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
                />
              </div>
              <input
                type="text"
                value={editForm.tags}
                onChange={(e) => handleEditChange("tags", e.target.value)}
                placeholder="Tags (cách nhau bằng dấu phẩy)"
                className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
              />

              <input
                type="file"
                multiple
                accept="image/*"
                onChange={handleImageChange}
                className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
              />
              <p className="text-xs text-gray-500">
                Chỉ upload ảnh mới nếu bạn muốn thay đổi toàn bộ ảnh hiện tại.
              </p>
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={closeEditModal}
                className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-semibold text-gray-700"
              >
                Hủy
              </button>
              <button
                type="button"
                disabled={isUpdatingSuggestion}
                onClick={submitEditSuggestion}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
              >
                {isUpdatingSuggestion ? "Đang gửi..." : "Gửi đề xuất"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserSubmissionsTab;
