import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Calendar, Clock, MapPin } from "lucide-react";
import { TimePickerDropdown } from "../../../common/inputs/TimePickerDropdown";
import toast from "../../../../utils/toast";

const AddToScheduleModal = ({
  isOpen,
  onClose,
  onConfirm,
  days = [],
  itemTitle,
}) => {
  const [selectedDayId, setSelectedDayId] = useState("");
  const [startTime, setStartTime] = useState("01:00");
  const [endTime, setEndTime] = useState("02:00");

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen && days.length > 0) {
      setSelectedDayId(days[0].id);
      setStartTime("01:00");
      setEndTime("02:00");
    }
  }, [isOpen, days]);

  // Auto-adjust endTime when startTime changes to ensure startTime < endTime
  const handleStartTimeChange = (newStartTime) => {
    setStartTime(newStartTime);
    const [startH, startM] = newStartTime.split(":").map(Number);
    const [endH, endM] = endTime.split(":").map(Number);
    const startMinutes = startH * 60 + startM;
    const endMinutes = endH * 60 + endM;

    // If endTime <= startTime, auto-push endTime to startTime + 1 hour
    if (endMinutes <= startMinutes) {
      const newEndMinutes = Math.min(startMinutes + 60, 23 * 60 + 59);
      const newEndH = Math.floor(newEndMinutes / 60).toString().padStart(2, "0");
      const newEndM = (newEndMinutes % 60).toString().padStart(2, "0");
      setEndTime(`${newEndH}:${newEndM}`);
    }
  };

  if (!isOpen) return null;

  const handleConfirm = () => {
    if (selectedDayId) {
      // Validate Time: endTime must be strictly greater than startTime
      const [startH, startM] = startTime.split(":").map(Number);
      const [endH, endM] = endTime.split(":").map(Number);
      const startMinutes = startH * 60 + startM;
      const endMinutes = endH * 60 + endM;

      if (endMinutes <= startMinutes) {
        toast.error(
          "Thời gian kết thúc phải sau thời gian bắt đầu",
        );
        return;
      }

      onConfirm(selectedDayId, startTime, endTime);
    }
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-80 flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        />

        {/* Modal */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative bg-white rounded-2xl shadow-xl w-full max-w-md z-10"
        >
          {/* Header */}
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-bold text-lg text-slate-800">
              Thêm vào lịch trình
            </h3>
            <button
              onClick={onClose}
              className="p-2 -mr-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-full transition-colors"
            >
              <X size={20} />
            </button>
          </div>

          <div className="p-6 space-y-6">
            {/* Item Info Preview */}
            <div className="flex items-start gap-3 p-3 bg-blue-50/50 rounded-xl border border-blue-100/50">
              <div className="mt-0.5 min-w-[20px]">
                <MapPin size={18} className="text-blue-600" />
              </div>
              <div>
                <h4 className="font-bold text-gray-800 text-sm line-clamp-1">
                  {itemTitle}
                </h4>
                <p className="text-xs text-blue-600/80 mt-0.5 font-medium">
                  Chuyển từ Ý tưởng sang Lịch trình
                </p>
              </div>
            </div>

            {/* Day Selection */}
            <div className="space-y-3">
              <label className="text-sm font-bold text-slate-700 flex items-center gap-2">
                <Calendar size={16} className="text-slate-400" />
                Chọn ngày
              </label>
              <div className="grid grid-cols-1 gap-2 max-h-[160px] overflow-y-auto p-1">
                {days.map((day) => (
                  <button
                    key={day.id}
                    onClick={() => setSelectedDayId(day.id)}
                    className={`group flex items-center justify-between p-3 rounded-xl border transition-all duration-200 ${
                      selectedDayId === day.id
                        ? "border-transparent bg-linear-to-r from-blue-500/10 to-cyan-500/10 shadow-sm ring-1 ring-blue-500/50"
                        : "border-gray-100 hover:border-blue-200 hover:bg-gray-50"
                    }`}
                  >
                    <span
                      className={`font-semibold text-sm ${
                        selectedDayId === day.id
                          ? "text-blue-700"
                          : "text-slate-600 group-hover:text-slate-900"
                      }`}
                    >
                      {day.label}
                    </span>

                    <div
                      className={`w-5 h-5 rounded-full border flex items-center justify-center transition-colors ${
                        selectedDayId === day.id
                          ? "border-transparent bg-linear-to-r from-blue-500 to-cyan-500 text-white shadow-md"
                          : "border-slate-300 bg-white"
                      }`}
                    >
                      {selectedDayId === day.id && (
                        <motion.div
                          initial={{ scale: 0 }}
                          animate={{ scale: 1 }}
                          className="w-2 h-2 bg-white rounded-full"
                        />
                      )}
                    </div>
                  </button>
                ))}
              </div>
            </div>

            {/* Time Selection */}
            <div className="space-y-3">
              <label className="text-sm font-bold text-gray-700 flex items-center gap-2">
                <Clock size={16} className="text-gray-400" />
                <span>
                  Thời gian dự kiến
                  {startTime && endTime && (
                    <span className="ml-1 text-blue-600 font-normal">
                      (
                      {(() => {
                        const [startH, startM] = startTime
                          .split(":")
                          .map(Number);
                        const [endH, endM] = endTime.split(":").map(Number);
                        let diffMinutes =
                          endH * 60 + endM - (startH * 60 + startM);
                        if (diffMinutes < 0) diffMinutes += 24 * 60; // Handle overnight

                        const h = Math.floor(diffMinutes / 60);
                        const m = diffMinutes % 60;

                        if (h === 0 && m === 0) return "0h";
                        return `${h > 0 ? h + "h" : ""}${m > 0 ? " " + m + "m" : ""}`;
                      })()}
                      )
                    </span>
                  )}
                </span>
              </label>
              <div className="flex items-center gap-4">
                <TimePickerDropdown
                  value={startTime}
                  onChange={handleStartTimeChange}
                  label="Bắt đầu"
                />
                <div className="mt-5 text-gray-300">
                  <svg
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M5 12h14" />
                    <path d="m12 5 7 7-7 7" />
                  </svg>
                </div>
                <TimePickerDropdown
                  value={endTime}
                  onChange={setEndTime}
                  label="Kết thúc"
                />
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-slate-100 flex gap-3 bg-slate-50/50 rounded-2xl">
            <button
              onClick={onClose}
              className="flex-1 px-4 py-2.5 bg-white border border-slate-200 text-slate-700 rounded-xl font-bold text-sm hover:bg-slate-50 hover:border-slate-300 transition-colors shadow-sm"
            >
              Hủy
            </button>
            <button
              onClick={handleConfirm}
              disabled={!selectedDayId}
              className={`flex-1 px-4 py-2.5 text-white rounded-xl font-bold text-sm shadow-md transition-all active:scale-95 ${
                selectedDayId
                  ? "bg-linear-to-r from-blue-500 to-cyan-500 hover:shadow-lg hover:shadow-blue-500/30"
                  : "bg-slate-300 cursor-not-allowed shadow-none"
              }`}
            >
              Thêm vào lịch trình
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default AddToScheduleModal;
