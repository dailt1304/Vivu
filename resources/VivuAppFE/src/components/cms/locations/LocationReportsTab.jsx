import React, { useState, useMemo } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  Eye,
  MoreHorizontal,
  CheckCircle,
  XCircle,
  MapPin,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import DataTable from "@/components/cms/common/DataTable";
import StatusBadge from "@/components/cms/common/StatusBadge";
import DetailSheet from "@/components/cms/common/DetailSheet";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  useLocationReports,
  useReviewReport,
} from "@/hooks/locations/useLocationReports";
import useDebounce from "@/hooks/utils/useDebounce";
import toast from "@/utils/toast";

const LocationReportsTab = () => {
  const [activeTab, setActiveTab] = useState("all");
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 300);

  // Map tab to API status
  const apiStatus = useMemo(() => {
    if (activeTab === "all") return "";
    return activeTab.toUpperCase();
  }, [activeTab]);

  const {
    data: reportsRes,
    isLoading,
    mutate,
  } = useLocationReports({
    pageNumber: 1,
    pageSize: 100,
    status: apiStatus,
  });

  const [previewImage, setPreviewImage] = useState(null);

  const { trigger: reviewReport } = useReviewReport();

  // Handle edit form for WRONG_INFO reports
  const [editFormData, setEditFormData] = useState({
    name: "",
    description: "",
    images: "",
    adminNote: "",
  });

  // Reset form when selection changes
  const handleViewReport = (report) => {
    setSelectedLocation(report);
    setEditFormData({
      name: report.suggestedName || report.locationName || "",
      description: report.description || "",
      address: report.suggestedAddress || "",
      images: "", // Images aren't in DTO yet, placeholder
      adminNote: report.adminNote || "",
    });
    setSheetOpen(true);
  };

  const [sortConfig, setSortConfig] = useState({
    key: "createdDate",
    direction: "desc",
  });

  const loading = isLoading;
  const reports = useMemo(
    () => reportsRes?.items || reportsRes || [],
    [reportsRes],
  );

  const columns = [
    {
      key: "locationName",
      label: "Địa điểm",
      sortable: true,
      render: (value, row) => (
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-gray-100 flex items-center justify-center">
            <MapPin className="h-5 w-5 text-gray-500" />
          </div>
          <div>
            <p className="font-medium text-gray-900">{value}</p>
            <p className="text-xs text-gray-500 truncate max-w-[150px]">
              {row.locationAddress}
            </p>
          </div>
        </div>
      ),
    },
    {
      key: "reportType",
      label: "Loại báo cáo",
      render: (value) => (
        <span className="text-xs font-medium px-2 py-1 rounded bg-slate-100 text-slate-700">
          {value === "WRONG_INFO"
            ? "Sai thông tin"
            : value === "CLOSED"
              ? "Đã đóng cửa"
              : "Khác"}
        </span>
      ),
    },
    {
      key: "reporterName",
      label: "Người báo cáo",
      render: (value, row) => (
        <div className="flex items-center gap-2">
          <Avatar className="h-7 w-7">
            <AvatarImage src={row.reporterAvatar} />
            <AvatarFallback className="bg-primary/10 text-primary text-xs">
              {value?.charAt(0) || "U"}
            </AvatarFallback>
          </Avatar>
          <span className="text-sm">{value || "Ẩn danh"}</span>
        </div>
      ),
    },
    {
      key: "status",
      label: "Trạng thái",
      render: (value) => <StatusBadge status={value} />,
    },
    {
      key: "createdDate",
      label: "Ngày gửi",
      sortable: true,
      render: (value) => new Date(value).toLocaleDateString("vi-VN"),
    },
  ];

  const filteredData = useMemo(() => {
    let result = reports.filter(
      (report) => report.reportType !== "NEW_LOCATION",
    );

    if (debouncedSearch) {
      result = result.filter(
        (loc) =>
          loc.locationName
            ?.toLowerCase()
            .includes(debouncedSearch.toLowerCase()) ||
          loc.reason?.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          loc.reporterName
            ?.toLowerCase()
            .includes(debouncedSearch.toLowerCase()),
      );
    }

    if (sortConfig) {
      result.sort((a, b) => {
        const aVal = a[sortConfig.key];
        const bVal = b[sortConfig.key];
        if (sortConfig.direction === "asc") {
          return aVal > bVal ? 1 : -1;
        }
        return aVal < bVal ? 1 : -1;
      });
    }

    return result;
  }, [debouncedSearch, sortConfig, reports]);

  const handleSort = (key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  };

  const handleApprove = async () => {
    try {
      await reviewReport({
        reportId: selectedLocation.id,
        status: 2, // APPROVED
        adminNote: editFormData.adminNote,
        name: editFormData.name,
        description: editFormData.description,
        images: editFormData.images,
      });
      toast.success("Đã phê duyệt báo cáo");
      mutate();
      setSheetOpen(false);
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi phê duyệt báo cáo");
    }
  };

  const handleReject = async () => {
    if (!editFormData.adminNote) {
      toast.error("Vui lòng nhập lý do từ chối vào mục Ghi chú");
      return;
    }

    try {
      await reviewReport({
        reportId: selectedLocation.id,
        status: 3, // REJECTED
        adminNote: editFormData.adminNote,
      });
      toast.success("Đã từ chối báo cáo");
      mutate();
      setSheetOpen(false);
    } catch (e) {
      console.error(e);
      toast.error("Lỗi khi từ chối báo cáo");
    }
  };

  const renderActions = (row) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => handleViewReport(row)}>
          <Eye className="h-4 w-4 mr-2" />
          Xem chi tiết
        </DropdownMenuItem>
        {row.status.toUpperCase() === "PENDING" && (
          <>
            <DropdownMenuItem
              onClick={() => handleViewReport(row)}
              className="text-green-600"
            >
              <CheckCircle className="h-4 w-4 mr-2" />
              Phê duyệt / Từ chối
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="space-y-6"
    >
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="all">Tất cả</TabsTrigger>
          <TabsTrigger value="pending">Chờ xử lý</TabsTrigger>
          <TabsTrigger value="approved">Đã phê duyệt</TabsTrigger>
          <TabsTrigger value="rejected">Đã từ chối</TabsTrigger>
        </TabsList>
      </Tabs>

      <DataTable
        columns={columns}
        data={filteredData}
        loading={loading}
        searchPlaceholder="Tìm kiếm báo cáo..."
        onSearch={setSearchQuery}
        sortConfig={sortConfig}
        onSort={handleSort}
        actions={renderActions}
        pagination={{
          currentPage: reportsRes?.pageNumber || 1,
          totalPages: reportsRes?.totalPages || 1,
          from: (reportsRes?.pageNumber - 1) * reportsRes?.pageSize + 1 || 1,
          to:
            Math.min(
              reportsRes?.pageNumber * reportsRes?.pageSize,
              reportsRes?.totalCount,
            ) || filteredData.length,
          total: reportsRes?.totalCount || filteredData.length,
        }}
        onPageChange={(page) => {
          console.log("Change to page", page);
        }}
        emptyMessage="Không có báo cáo nào"
      />

      <DetailSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        title="Chi tiết Báo cáo Địa điểm"
        description={selectedLocation?.locationName}
      >
        {selectedLocation && (
          <div className="space-y-6 pb-20">
            <div>
              <h4 className="text-sm font-medium text-gray-500 mb-1">
                Loại báo cáo
              </h4>
              <p className="font-medium">
                {selectedLocation.reportType === "WRONG_INFO"
                  ? "Sai thông tin"
                  : selectedLocation.reportType === "CLOSED"
                    ? "Đã đóng cửa"
                    : "Khác"}
              </p>
            </div>

            <div>
              <h4 className="text-sm font-medium text-gray-500 mb-1">Lý do</h4>
              <p className="bg-orange-50 text-orange-800 p-3 rounded-md text-sm italic">
                "{selectedLocation.reason}"
              </p>
            </div>

            {selectedLocation.description && (
              <div>
                <h4 className="text-sm font-medium text-gray-500 mb-1">
                  Mô tả chi tiết từ người dùng
                </h4>
                <p className="text-gray-600 bg-gray-50 p-3 rounded-md text-sm">
                  {selectedLocation.description}
                </p>
              </div>
            )}

            {/* Evidence Images */}
            {selectedLocation.evidenceImages &&
              (() => {
                try {
                  const images = JSON.parse(selectedLocation.evidenceImages);
                  if (images && images.length > 0) {
                    return (
                      <div className="border-t pt-6">
                        <h4 className="text-sm font-medium text-gray-900 mb-4 uppercase tracking-wider">
                          Ảnh bằng chứng từ người báo cáo
                        </h4>
                        <div className="grid grid-cols-3 gap-3">
                          {images.map((img, i) => (
                            <div
                              key={i}
                              className="relative aspect-video rounded-xl overflow-hidden border border-gray-200 cursor-pointer group"
                              onClick={() => setPreviewImage(img)}
                            >
                              <img
                                src={img}
                                alt={`Evidence ${i + 1}`}
                                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                              />
                              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/10 transition-colors" />
                            </div>
                          ))}
                        </div>
                      </div>
                    );
                  }
                } catch (e) {
                  return null;
                }
                return null;
              })()}

            <div className="border-t pt-6">
              <h4 className="text-sm font-medium text-gray-900 mb-4 uppercase tracking-wider">
                Thông tin Địa điểm bị báo cáo
              </h4>
              <div className="space-y-4">
                <div>
                  <h5 className="text-xs font-medium text-gray-500 mb-1">
                    Tên địa điểm
                  </h5>
                  <p>{selectedLocation.locationName}</p>
                </div>
                <div>
                  <h5 className="text-xs font-medium text-gray-500 mb-1">
                    Địa chỉ
                  </h5>
                  <p className="text-sm text-gray-600">
                    {selectedLocation.locationAddress}
                  </p>
                </div>
              </div>

              {(selectedLocation.suggestedName ||
                selectedLocation.suggestedAddress) && (
                <div className="mt-4 bg-emerald-50 p-4 rounded-lg border border-emerald-200">
                  <p className="text-xs font-bold text-emerald-700 uppercase mb-2">
                    Đề xuất từ người báo cáo
                  </p>
                  {selectedLocation.suggestedName && (
                    <p className="text-sm">
                      <strong>Tên:</strong> {selectedLocation.suggestedName}
                    </p>
                  )}
                  {selectedLocation.suggestedAddress && (
                    <p className="text-sm mt-1">
                      <strong>Địa chỉ:</strong>{" "}
                      {selectedLocation.suggestedAddress}
                    </p>
                  )}
                </div>
              )}
            </div>

            <div className="border-t pt-6">
              <h4 className="text-sm font-medium text-gray-900 mb-4 uppercase tracking-wider">
                Thông tin Người báo cáo
              </h4>
              <div className="flex items-center gap-3">
                <Avatar className="h-10 w-10">
                  <AvatarImage src={selectedLocation.reporterAvatar} />
                  <AvatarFallback className="bg-primary/10 text-primary">
                    {selectedLocation.reporterName?.charAt(0) || "U"}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-sm">
                    {selectedLocation.reporterName || "Người dùng ẩn danh"}
                  </p>
                  <p className="text-xs text-gray-500">
                    {selectedLocation.reporterEmail}
                  </p>
                </div>
              </div>
            </div>

            {selectedLocation.status.toUpperCase() === "PENDING" ? (
              <div className="border-t pt-6 space-y-4">
                <h4 className="text-sm font-medium text-gray-900 mb-4 uppercase tracking-wider">
                  Xử lý Báo cáo
                </h4>

                {selectedLocation.reportType === "WRONG_INFO" && (
                  <div className="space-y-3 bg-blue-50/50 p-4 rounded-lg border border-blue-100">
                    <p className="text-xs font-bold text-blue-700 uppercase">
                      Cập nhật thông tin địa điểm (Nếu phê duyệt)
                    </p>
                    <div className="space-y-3">
                      <div>
                        <label className="text-xs text-gray-500">
                          Tên địa điểm mới
                        </label>
                        <input
                          type="text"
                          className="w-full text-sm p-2 border rounded"
                          value={editFormData.name}
                          onChange={(e) =>
                            setEditFormData({
                              ...editFormData,
                              name: e.target.value,
                            })
                          }
                        />
                      </div>
                      <div>
                        <label className="text-xs text-gray-500">
                          Mô tả mới
                        </label>
                        <textarea
                          className="w-full text-sm p-2 border rounded h-24"
                          value={editFormData.description}
                          onChange={(e) =>
                            setEditFormData({
                              ...editFormData,
                              description: e.target.value,
                            })
                          }
                        />
                      </div>
                    </div>
                  </div>
                )}

                {selectedLocation.reportType === "CLOSED" && (
                  <div className="bg-red-50 p-4 rounded-lg border border-red-200">
                    <p className="text-sm font-semibold text-red-800">
                      Phê duyệt báo cáo này sẽ ẩn địa điểm khỏi hệ thống
                      (soft-delete).
                    </p>
                    <p className="text-xs text-red-600 mt-1">
                      Địa điểm sẽ không còn xuất hiện trong kết quả tìm kiếm.
                    </p>
                  </div>
                )}

                {selectedLocation.reportType === "OTHER" && (
                  <div className="bg-slate-50 p-4 rounded-lg border border-slate-200">
                    <p className="text-sm text-slate-600">
                      Loại báo cáo "Khác" — Vui lòng xem xét lý do và mô tả chi
                      tiết ở trên.
                    </p>
                  </div>
                )}

                <div>
                  <label className="text-xs font-medium text-gray-500 mb-1 block">
                    Ghi chú của Admin (Lý do từ chối hoặc phản hồi)
                  </label>
                  <textarea
                    className="w-full text-sm p-2 border rounded h-20"
                    placeholder="Nhập ghi chú..."
                    value={editFormData.adminNote}
                    onChange={(e) =>
                      setEditFormData({
                        ...editFormData,
                        adminNote: e.target.value,
                      })
                    }
                  />
                </div>

                <div className="flex gap-3 pt-4 sticky bottom-0 bg-white">
                  <Button onClick={handleApprove} className="flex-1 bg-blue-500 hover:bg-blue-600 text-white">
                    <CheckCircle className="h-4 w-4 mr-2" />
                    Phê duyệt & Cập nhật
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={handleReject}
                    className="flex-1"
                  >
                    <XCircle className="h-4 w-4 mr-2" />
                    Từ chối
                  </Button>
                </div>
              </div>
            ) : (
              <div className="border-t pt-6">
                <h4 className="text-sm font-medium text-gray-500 mb-1">
                  Ghi chú của Admin đã xử lý
                </h4>
                <p className="text-sm bg-gray-50 p-3 rounded border">
                  {selectedLocation.adminNote || "(Không có ghi chú)"}
                </p>
              </div>
            )}
          </div>
        )}
      </DetailSheet>

      {/* Fullscreen Image Preview */}
      <AnimatePresence>
        {previewImage && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setPreviewImage(null)}
            className="fixed inset-0 bg-black/90 z-[9999] flex items-center justify-center p-4 cursor-zoom-out"
          >
            <button
              onClick={() => setPreviewImage(null)}
              className="absolute top-4 right-4 text-white/70 hover:text-white p-2"
            >
              <XCircle size={32} />
            </button>
            <img
              src={previewImage}
              alt="Fullscreen preview"
              className="max-w-full max-h-[90vh] object-contain rounded-lg"
              onClick={(e) => e.stopPropagation()}
            />
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};

export default LocationReportsTab;
