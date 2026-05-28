import React, { useState, useMemo } from "react";
import { motion } from "framer-motion";
import { CheckCircle, XCircle, MapPin, Eye } from "lucide-react";
import { Button } from "@/components/ui/button";
import DataTable from "@/components/cms/common/DataTable";
import DetailSheet from "@/components/cms/common/DetailSheet";
import StatusBadge from "@/components/cms/common/StatusBadge";
import {
  usePendingLocations,
  useApproveLocation,
  useRejectLocation,
} from "@/hooks/locations/useLocationManagement";
import useDebounce from "@/hooks/utils/useDebounce";
import toast from "@/utils/toast";

const PendingLocationsTab = () => {
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 300);
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");

  const {
    data: pendingRes,
    isLoading,
    mutate,
  } = usePendingLocations({
    pageNumber: 1,
    pageSize: 100,
  });

  const { trigger: approveLocation } = useApproveLocation();
  const { trigger: rejectLocation } = useRejectLocation();

  const [sortConfig, setSortConfig] = useState({
    key: "createdAt",
    direction: "desc",
  });

  const locations = useMemo(
    () => {
      const items = pendingRes?.items || pendingRes || [];
      // Safety net: chỉ giữ locations có ít nhất 1 report kiểu NEW_LOCATION đang PENDING
      // (Backend đã lọc chính, đây là lớp bảo vệ phía client)
      return items.filter(loc => {
        const reports = loc.locationReports || [];
        if (reports.length === 0) return true; // Nếu API không trả nested reports thì tin tưởng BE
        return reports.some(r =>
          r.reportType === 'NEW_LOCATION' &&
          r.status?.toUpperCase() === 'PENDING'
        );
      });
    },
    [pendingRes],
  );

  const columns = [
    {
      key: "name",
      label: "Địa điểm",
      sortable: true,
      render: (value, row) => (
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-lg bg-gray-100 flex items-center justify-center shrink-0">
            <MapPin className="h-5 w-5 text-gray-500" />
          </div>
          <div>
            <p className="font-medium text-gray-900">{value}</p>
            <p className="text-xs text-gray-500 truncate max-w-[200px]">
              {row.address}
            </p>
          </div>
        </div>
      ),
    },
    {
      key: "categoryName",
      label: "Danh mục",
      render: (_, row) => (
        <span className="text-xs font-medium px-2 py-1 rounded bg-blue-50 text-blue-700">
          {row.category?.name || row.categoryName || "Chưa phân loại"}
        </span>
      ),
    },
    {
      key: "status",
      label: "Trạng thái",
      render: () => <StatusBadge status="PENDING" />,
    },
    {
      key: "createdDate",
      label: "Ngày gửi",
      sortable: true,
      render: (value) => value ? new Date(value).toLocaleDateString("vi-VN") : "N/A",
    },
    {
      key: "actions",
      label: "Thao tác",
      render: (_, row) => (
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            setSelectedLocation(row);
            setRejectReason("");
            setSheetOpen(true);
          }}
        >
          <Eye className="h-4 w-4 mr-2" />
          Xem xét duyệt
        </Button>
      ),
    },
  ];

  const filteredData = useMemo(() => {
    let result = [...locations];

    if (debouncedSearch) {
      result = result.filter(
        (loc) =>
          loc.name?.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          loc.address?.toLowerCase().includes(debouncedSearch.toLowerCase()),
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
  }, [debouncedSearch, sortConfig, locations]);

  const handleSort = (key) => {
    setSortConfig((prev) => ({
      key,
      direction: prev.key === key && prev.direction === "asc" ? "desc" : "asc",
    }));
  };

  const handleApprove = async () => {
    try {
      await approveLocation({
        id: selectedLocation.id,
        data: { adminNote: rejectReason || "Đã phê duyệt địa điểm" },
      });
      toast.success("Đã duyệt địa điểm thành công");
      mutate();
      setSheetOpen(false);
    } catch (error) {
      console.error(error);
      toast.error("Lỗi khi duyệt địa điểm");
    }
  };

  const handleReject = async () => {
    if (!rejectReason) {
      toast.error("Vui lòng nhập lý do từ chối");
      return;
    }
    try {
      await rejectLocation({
        id: selectedLocation.id,
        data: { adminNote: rejectReason },
      });
      toast.success("Đã từ chối địa điểm");
      mutate();
      setSheetOpen(false);
    } catch (error) {
      console.error(error);
      toast.error("Lỗi khi từ chối địa điểm");
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
      className="space-y-4"
    >
      <DataTable
        columns={columns}
        data={filteredData}
        loading={isLoading}
        searchPlaceholder="Tìm kiếm địa điểm chờ duyệt..."
        onSearch={setSearchQuery}
        sortConfig={sortConfig}
        onSort={handleSort}
        pagination={{
          currentPage: pendingRes?.pageNumber || 1,
          totalPages: pendingRes?.totalPages || 1,
          from: (pendingRes?.pageNumber - 1) * pendingRes?.pageSize + 1 || 1,
          to:
            Math.min(
              pendingRes?.pageNumber * pendingRes?.pageSize,
              pendingRes?.totalCount,
            ) || filteredData.length,
          total: pendingRes?.totalCount || filteredData.length,
        }}
        emptyMessage="Không có địa điểm nào đang chờ duyệt"
      />

      <DetailSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        title="Duyệt Địa Điểm Mới"
        description="Xem xét và quyết định phê duyệt địa điểm do người dùng đề xuất"
      >
        {selectedLocation && (
          <div className="space-y-6 pb-20">
            {/* Location Info */}
            <div className="space-y-4">
              <div>
                <h4 className="text-sm font-medium text-gray-500 mb-1">
                  Tên địa điểm
                </h4>
                <p className="text-lg font-bold text-gray-900">
                  {selectedLocation.name}
                </p>
              </div>

              <div>
                <h4 className="text-sm font-medium text-gray-500 mb-1">
                  Danh mục
                </h4>
                <p className="inline-block px-2.5 py-1 bg-blue-50 text-blue-700 rounded-md text-sm font-medium">
                  {selectedLocation.category?.name || selectedLocation.categoryName || "Chưa phân loại"}
                </p>
              </div>

              <div>
                <h4 className="text-sm font-medium text-gray-500 mb-1">
                  Địa chỉ
                </h4>
                <div className="flex gap-2 text-gray-700 bg-gray-50 p-3 rounded-lg border border-gray-100">
                  <MapPin className="shrink-0 h-5 w-5 text-gray-400" />
                  <p className="text-sm leading-relaxed">
                    {selectedLocation.address}
                  </p>
                </div>
              </div>

              {selectedLocation.description && (
                <div>
                  <h4 className="text-sm font-medium text-gray-500 mb-1">
                    Mô tả (từ người gửi)
                  </h4>
                  <p className="text-sm text-gray-700 bg-gray-50 p-3 rounded-lg border border-gray-100 whitespace-pre-wrap">
                    {selectedLocation.description}
                  </p>
                </div>
              )}

              {/* Thông tin thêm từ LocationDetail */}
              {selectedLocation.locationDetail && (
                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <h4 className="text-sm font-medium text-gray-500 mb-1">
                        Số điện thoại
                      </h4>
                      <div className="bg-gray-50 p-2.5 rounded-lg border border-gray-100 text-sm font-medium text-gray-900 break-all">
                        {selectedLocation.locationDetail.phone || "Chưa cung cấp"}
                      </div>
                    </div>
                    <div>
                      <h4 className="text-sm font-medium text-gray-500 mb-1">
                        Lịch hoạt động
                      </h4>
                      <div className="bg-gray-50 p-2.5 rounded-lg border border-gray-100 text-sm font-medium text-gray-900 break-all">
                        {(() => {
                          const raw = selectedLocation.locationDetail?.openingHours;
                          if (!raw) return <span className="text-gray-400 italic">Chưa cung cấp</span>;
                          
                          try {
                            if (raw.trim().startsWith("{")) {
                              const parsed = JSON.parse(raw);
                              const DAYS = [
                                { id: "monday", label: "Thứ Hai" },
                                { id: "tuesday", label: "Thứ Ba" },
                                { id: "wednesday", label: "Thứ Tư" },
                                { id: "thursday", label: "Thứ Năm" },
                                { id: "friday", label: "Thứ Sáu" },
                                { id: "saturday", label: "Thứ Bảy" },
                                { id: "sunday", label: "Chủ Nhật" },
                              ];
                              return (
                                <div className="space-y-1">
                                  {DAYS.map(d => (
                                    <div key={d.id} className="flex justify-between items-center whitespace-nowrap gap-2">
                                      <span className="text-gray-500 font-medium text-xs w-16">{d.label}</span>
                                      <span className={`font-semibold text-xs ${parsed[d.id] === "closed" ? "text-red-500" : "text-gray-900"} bg-white px-2 py-0.5 rounded border border-gray-100 flex-1 text-right`}>
                                        {parsed[d.id] === "closed" ? "Đóng cửa" : parsed[d.id] || "—"}
                                      </span>
                                    </div>
                                  ))}
                                </div>
                              );
                            }
                          } catch {}
                          
                          // Fallback
                          return <span>{raw}</span>;
                        })()}
                      </div>
                    </div>
                  </div>

                  <div>
                    <h4 className="text-sm font-medium text-gray-500 mb-1">
                      Website / Fanpage
                    </h4>
                    <div className="bg-gray-50 p-2.5 rounded-lg border border-gray-100 text-sm">
                      {selectedLocation.locationDetail.website ? (
                        <a href={selectedLocation.locationDetail.website} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline break-all font-medium">
                          {selectedLocation.locationDetail.website}
                        </a>
                      ) : (
                        <span className="text-gray-500 italic">Chưa cung cấp</span>
                      )}
                    </div>
                  </div>

                  <div>
                    <h4 className="text-sm font-medium text-gray-500 mb-1">
                      Từ khóa (Tags)
                    </h4>
                    <div className="bg-gray-50 p-2.5 rounded-lg border border-gray-100 text-sm text-gray-700">
                      {selectedLocation.locationDetail.tags || <span className="text-gray-400 italic">Không có từ khóa</span>}
                    </div>
                  </div>

                  {/* Hình ảnh đính kèm */}
                  {selectedLocation.locationDetail.images && (
                    <div>
                      <h4 className="text-sm font-medium text-gray-500 mb-2">
                        Hình ảnh đính kèm
                      </h4>
                      <div className="grid grid-cols-3 gap-2">
                        {(() => {
                          try {
                            const parsedImages = JSON.parse(selectedLocation.locationDetail.images);
                            if (Array.isArray(parsedImages) && parsedImages.length > 0) {
                              return parsedImages.map((img, idx) => (
                                <a key={idx} href={img} target="_blank" rel="noreferrer" className="block aspect-[4/3] rounded-lg overflow-hidden border border-gray-200 hover:opacity-90 transition-opacity">
                                  <img src={img} alt={`Hình ${idx + 1}`} className="w-full h-full object-cover" />
                                </a>
                              ));
                            }
                            return <p className="col-span-3 text-sm text-gray-400 italic">Không có ảnh hợp lệ</p>;
                          } catch (e) {
                             return <p className="col-span-3 text-sm text-gray-400 italic">Lỗi định dạng ảnh</p>;
                          }
                        })()}
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Coordinates */}
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-gray-50 p-3 rounded-lg border border-gray-100">
                  <span className="text-xs text-gray-500 block mb-1">
                    Vĩ độ (Latitude)
                  </span>
                  <span className="font-mono text-sm text-gray-800">
                    {selectedLocation.latitude || "N/A"}
                  </span>
                </div>
                <div className="bg-gray-50 p-3 rounded-lg border border-gray-100">
                  <span className="text-xs text-gray-500 block mb-1">
                    Kinh độ (Longitude)
                  </span>
                  <span className="font-mono text-sm text-gray-800">
                    {selectedLocation.longitude || "N/A"}
                  </span>
                </div>
              </div>
            </div>

            {/* Approval/Rejection Actions */}
            <div className="border-t pt-6 space-y-4">
              <h4 className="font-semibold text-gray-900 border-l-4 border-blue-500 pl-2">
                Quyết định Xét Duyệt
              </h4>

              <div>
                <label className="text-sm font-medium text-gray-700 block mb-2">
                  Ghi chú nội bộ / Lý do từ chối (bắt buộc nếu từ chối)
                </label>
                <textarea
                  className="w-full text-sm p-3 border border-gray-200 rounded-xl min-h-[100px] focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all"
                  placeholder="Nhập ghi chú hoặc lý do từ chối địa điểm này..."
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                />
              </div>

              <div className="flex gap-3 pt-4 sticky bottom-0 bg-white">
                <Button onClick={handleApprove} className="flex-1 bg-blue-500 hover:bg-blue-600 text-white">
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Phê duyệt
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
          </div>
        )}
      </DetailSheet>
    </motion.div>
  );
};

export default PendingLocationsTab;
