import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Calendar, Check, AlertTriangle } from "lucide-react";
import { format, differenceInDays } from "date-fns";
import { vi } from "date-fns/locale";
import DateRangePicker from "../../../common/inputs/DateRangePicker";

const EditDateModal = ({
  isOpen,
  onClose,
  startDate,
  endDate,
  onSave,
  occupiedDayIndices = [], // Array of day indices that have scheduled items
}) => {
  const [tempStartDate, setTempStartDate] = useState(startDate);
  const [tempEndDate, setTempEndDate] = useState(endDate);

  useEffect(() => {
    if (isOpen) {
      setTempStartDate(startDate);
      setTempEndDate(endDate);
    }
  }, [isOpen, startDate, endDate]);

  // Validation Logic: Check if any occupied day would be cut off
  // Backend calculates expectedTotalDays = (end - start).Days + 1
  // so newDuration (= differenceInDays) + 1 = total days that will remain.
  const newDuration =
    tempStartDate && tempEndDate
      ? differenceInDays(tempEndDate, tempStartDate)
      : 0;
  // Total days that will remain after saving (matches backend formula)
  const newTotalDays = newDuration + 1;

  const isInvalidDuration = tempStartDate && tempEndDate && newDuration <= 0;

  // Find which occupied days would be excluded by the new total day count
  // occupiedDayIndices are 1-based dayIndex values
  const affectedDays = occupiedDayIndices.filter(
    (dayIndex) => dayIndex > newTotalDays,
  );
  const isDestructive = affectedDays.length > 0;

  const handleSave = () => {
    if (isDestructive || isInvalidDuration) return;
    if (tempStartDate && tempEndDate) {
      onSave(tempStartDate, tempEndDate);
      // Do NOT toast here — handleDateChange in TripDetailPage manages success/error feedback
      onClose();
    }
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-[10000]"
          />

          {/* Modal */}
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 30 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 30 }}
            className="fixed inset-0 top-14 lg:top-16 m-auto w-[95vw] max-w-3xl h-fit max-h-[calc(100vh-80px)] lg:max-h-[calc(100vh-100px)] overflow-hidden bg-white rounded-3xl shadow-2xl z-[10050] flex flex-col"
          >
            {/* Header */}
            <div className="p-4 md:px-6 md:pt-5 md:pb-3 relative flex items-center justify-between border-b border-gray-100">
              <div>
                <h3 className="text-xl md:text-2xl font-bold text-slate-800 tracking-tight">
                  Chỉnh sửa thời gian
                </h3>
                <p className="text-xs md:text-sm text-slate-500 mt-0.5">
                  Chọn ngày bắt đầu và kết thúc cho chuyến đi
                </p>
              </div>
              <button
                onClick={onClose}
                className="p-2 bg-slate-50 hover:bg-slate-100 rounded-full text-slate-500 hover:text-slate-800 transition-colors"
                aria-label="Close"
              >
                <X size={20} />
              </button>
            </div>

            <div className="p-4 md:p-5 overflow-y-auto custom-scrollbar flex-1">
              {/* Compact Date Display */}
              <div className="flex items-center justify-between bg-slate-50 p-3 md:p-4 rounded-2xl border border-slate-100 mb-4">
                <div className="flex-1">
                  <p className="text-xs font-bold text-slate-400 uppercase mb-1">
                    Bắt đầu
                  </p>
                  <div className="flex items-center gap-2 text-slate-800 font-bold text-lg">
                    <Calendar size={18} className="text-blue-500" />
                    {tempStartDate
                      ? format(tempStartDate, "dd/MM/yyyy", { locale: vi })
                      : "..."}
                  </div>
                </div>

                <div className="mx-4 flex flex-col items-center">
                  <div className="w-16 h-0.5 bg-slate-200/60 rounded-full mb-1"></div>
                  <span className="text-[10px] font-bold text-slate-400 uppercase tracking-widest">
                    Đến
                  </span>
                </div>

                <div className="flex-1 text-right">
                  <p className="text-xs font-bold text-slate-400 uppercase mb-1">
                    Kết thúc
                  </p>
                  <div className="flex items-center justify-end gap-2 text-slate-800 font-bold text-lg">
                    {tempEndDate
                      ? format(tempEndDate, "dd/MM/yyyy", { locale: vi })
                      : "..."}
                    <Calendar size={18} className="text-cyan-500" />
                  </div>
                </div>
              </div>

              {/* Error / Warning Alert */}
              <AnimatePresence>
                {(isDestructive || isInvalidDuration) && (
                  <motion.div
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: "auto" }}
                    exit={{ opacity: 0, height: 0 }}
                    className="mb-6 overflow-hidden"
                  >
                    <div className="p-4 bg-red-50 text-red-700 rounded-xl border border-red-100 flex gap-4">
                      <div className="p-2 bg-red-100 rounded-lg h-fit shrink-0">
                        <AlertTriangle size={20} className="text-red-600" />
                      </div>
                      <div className="flex-1">
                        <h4 className="font-bold text-sm mb-1 text-red-800">
                          {isInvalidDuration ? "Lỗi cấu hình ngày" : "Không thể thay đổi"}
                        </h4>
                        <p className="text-sm text-red-700/80 leading-relaxed">
                          {isInvalidDuration ? (
                            "Chuyến đi phải kéo dài ít nhất 1 ngày (đi và về khác nhau)."
                          ) : (
                            <>
                              Thời gian mới ({newDuration} ngày) sẽ xóa mất lịch
                              trình ở{" "}
                              <strong>
                                {affectedDays.map((d) => `Ngày ${d}`).join(", ")}
                              </strong>
                              . Vui lòng xóa các địa điểm ở những ngày đó trước khi
                              thay đổi.
                            </>
                          )}
                        </p>
                      </div>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>

              {/* The Calendar */}
              <div className="flex justify-center">
                <DateRangePicker
                  startDate={tempStartDate}
                  endDate={tempEndDate}
                  onChange={(start, end) => {
                    setTempStartDate(start);
                    setTempEndDate(end);
                  }}
                />
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 md:p-6 border-t border-gray-100 bg-gray-50/50 rounded-b-3xl flex flex-wrap-reverse gap-3 justify-end items-center">
              <button
                onClick={onClose}
                className="w-full sm:w-auto px-6 py-3 rounded-xl text-slate-600 font-semibold hover:bg-white hover:shadow-sm border border-transparent hover:border-slate-200 transition-all text-center"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleSave}
                disabled={isDestructive || isInvalidDuration || !tempStartDate || !tempEndDate}
                className={`w-full sm:w-auto px-8 py-3 bg-linear-to-r from-blue-600 to-cyan-500 text-white font-bold rounded-xl shadow-lg shadow-blue-500/20 flex items-center justify-center gap-2 transition-all
                    ${
                      isDestructive || isInvalidDuration || !tempStartDate || !tempEndDate
                        ? "opacity-50 cursor-not-allowed grayscale"
                        : "hover:shadow-blue-500/40 hover:-translate-y-0.5 active:scale-95 active:shadow-sm"
                    }`}
              >
                <Check size={20} strokeWidth={2.5} />
                Lưu thay đổi
              </button>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
};

export default EditDateModal;
