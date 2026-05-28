import React, { useState } from "react";
import {
  ChevronDown,
  Calendar,
  AlertCircle,
  AlertTriangle,
} from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";

/**
 * TripSelectWithWarning
 * A dropdown to select a trip, with an AlertDialog warning users about data loss.
 * Vercel Rule: Uncontrolled Form/State co-location (modals should manage their own state to avoid bubbling)
 */
const TripSelectWithWarning = ({
  myCompletedTrips,
  selectedTripId,
  onConfirmChange,
  isEditMode,
}) => {
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [pendingTripId, setPendingTripId] = useState(null);

  const selectedTrip = myCompletedTrips.find((t) => t.id === selectedTripId);

  const handleSelect = (tripId) => {
    setIsDropdownOpen(false);
    // If it's the exact same trip, do nothing
    if (tripId === selectedTripId) return;

    // If there is already a trip selected, alert warning (preventing data loss)
    // Even if it's new blog, if they already fetched blocks, changing trip would wipe edits.
    if (selectedTripId) {
      setPendingTripId(tripId);
    } else {
      // First time selecting a trip
      onConfirmChange(tripId);
    }
  };

  const handleConfirm = () => {
    onConfirmChange(pendingTripId);
    setPendingTripId(null);
  };

  const handleCancel = () => {
    setPendingTripId(null);
  };

  return (
    <div className="bg-white rounded-2xl border border-gray-200 p-6">
      <h3 className="font-bold text-gray-900 mb-4 flex items-center gap-2">
        Chọn chuyến đi hoàn thành <span className="text-red-500">*</span>
      </h3>

      <div className="relative">
        <button
          type="button"
          onClick={() => !isEditMode && setIsDropdownOpen(!isDropdownOpen)}
          disabled={isEditMode}
          className={`w-full px-5 py-4 border rounded-xl flex items-center justify-between transition-all bg-white text-left ${
            isDropdownOpen
              ? "border-blue-500 ring-4 ring-blue-500/10 shadow-sm"
              : "border-gray-200 hover:border-blue-300 hover:shadow-sm"
          } ${isEditMode ? "opacity-70 cursor-not-allowed bg-slate-50" : ""}`}
        >
          {selectedTrip ? (
            <div className="flex flex-col">
              <span className="font-semibold text-gray-900 line-clamp-1">
                {selectedTrip.title}
              </span>
              <span className="text-xs flex items-center gap-1 text-gray-500 mt-1">
                <Calendar size={14} />
                {new Date(selectedTrip.createdAt).toLocaleDateString("vi-VN")}
              </span>
            </div>
          ) : (
            <span className="text-gray-500 font-medium whitespace-nowrap overflow-hidden text-ellipsis">
              -- Chọn chuyến đi --
            </span>
          )}
          {!isEditMode && (
            <ChevronDown
              className={`text-gray-400 transition-transform duration-300 shrink-0 ml-3 ${
                isDropdownOpen ? "rotate-180" : ""
              }`}
              size={20}
            />
          )}
        </button>

        <AnimatePresence>
          {isDropdownOpen && !isEditMode && (
            <>
              <div
                className="fixed inset-0 z-40"
                onClick={() => setIsDropdownOpen(false)}
              />
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.2 }}
                className="absolute z-50 w-full mt-2 bg-white border border-gray-200 rounded-xl shadow-xl max-h-64 overflow-y-auto overflow-x-hidden hide-scrollbar"
              >
                {myCompletedTrips.map((t) => (
                  <button
                    key={t.id}
                    type="button"
                    onClick={() => handleSelect(t.id)}
                    className={`w-full text-left px-5 py-3 transition-colors border-b border-gray-50 last:border-b-0 hover:bg-blue-50/50 flex flex-col ${
                      selectedTripId === t.id ? "bg-blue-50" : ""
                    }`}
                  >
                    <span
                      className={`font-medium line-clamp-1 ${
                        selectedTripId === t.id
                          ? "text-blue-600"
                          : "text-gray-800"
                      }`}
                    >
                      {t.title}
                    </span>
                    <span className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                      <Calendar size={13} />
                      {new Date(t.createdAt).toLocaleDateString("vi-VN")}
                    </span>
                  </button>
                ))}
              </motion.div>
            </>
          )}
        </AnimatePresence>
      </div>

      {myCompletedTrips.length === 0 && !isEditMode && (
        <p className="text-sm text-amber-600 mt-3 p-3 bg-amber-50 rounded-lg flex items-center gap-2">
          <AlertCircle size={16} className="shrink-0" />
          <span>
            Bạn chưa có chuyến đi nào hoàn thành. Vui lòng hoàn thành một chuyến
            đi trước khi tạo blog.
          </span>
        </p>
      )}

      {/* AlertDialog Warning Modal */}
      <AnimatePresence>
        {pendingTripId && (
          <div className="fixed inset-0 z-100 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              className="bg-white rounded-3xl w-full max-w-sm p-6 shadow-2xl overflow-hidden"
            >
              <div className="w-16 h-16 rounded-full bg-red-100 flex items-center justify-center mx-auto mb-4">
                <AlertTriangle
                  size={32}
                  className="text-red-500"
                  strokeWidth={2.5}
                />
              </div>
              <h3 className="text-xl font-black text-slate-900 text-center mb-2">
                Thay đổi chuyến đi?
              </h3>
              <p className="text-slate-500 text-center text-sm font-medium leading-relaxed mb-6">
                Bạn đang chuẩn bị chuyển dữ liệu sang một chuyến đi khác. Toàn
                bộ nội dung (mô tả, nhắc nhở, hình ảnh, quotes) bạn đã chỉnh sửa
                bên dưới sẽ bị{" "}
                <span className="text-red-500 font-bold">xóa bỏ hoàn toàn</span>{" "}
                và thay vào đó là cấu trúc gốc của chuyến đi mới. Bạn không thể
                hoàn tác thao tác này.
              </p>

              <div className="flex flex-col gap-2">
                <button
                  onClick={handleConfirm}
                  className="w-full py-3 bg-red-500 hover:bg-red-600 text-white rounded-xl font-bold transition-colors shadow-md shadow-red-500/20"
                >
                  Đồng ý thay đổi
                </button>
                <button
                  onClick={handleCancel}
                  className="w-full py-3 bg-slate-100 hover:bg-slate-200 text-slate-600 rounded-xl font-bold transition-colors"
                >
                  Hủy bỏ
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default TripSelectWithWarning;
