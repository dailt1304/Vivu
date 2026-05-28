import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Clock, MapPin } from "lucide-react";
import { TimePickerDropdown } from "../../../common/inputs/TimePickerDropdown";
import toast from "../../../../utils/toast";

const EditTimeModal = ({
  isOpen,
  onClose,
  onConfirm,
  initialStartTime,
  initialEndTime,
  itemTitle,
}) => {
  const [startTime, setStartTime] = useState(initialStartTime || "09:00");
  const [endTime, setEndTime] = useState(initialEndTime || "11:00");

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setStartTime(initialStartTime || "09:00");
      setEndTime(initialEndTime || "11:00");
    }
  }, [isOpen, initialStartTime, initialEndTime]);

  if (!isOpen) return null;

  const handleConfirm = () => {
    // Validate Time
    const [startH, startM] = startTime.split(":").map(Number);
    const [endH, endM] = endTime.split(":").map(Number);
    const startMinutes = startH * 60 + startM;
    const endMinutes = endH * 60 + endM;

    if (endMinutes < startMinutes) {
      toast.error("Thời gian kết thúc phải sau thời gian bắt đầu");
      return;
    }

    onConfirm(startTime, endTime);
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 top-14 lg:top-16 z-[10000] flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/40 backdrop-blur-sm -top-14 lg:-top-16"
        />

        {/* Modal */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative bg-white rounded-2xl shadow-xl w-full max-w-sm z-10 max-h-[calc(100vh-100px)] overflow-visible"
        >
          {/* Header */}
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
            <h3 className="font-bold text-lg text-slate-800">
              Chỉnh sửa thời gian
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
                  Điều chỉnh thời gian tham quan
                </p>
              </div>
            </div>

            {/* Time Selection */}
            <div className="space-y-3">
              <label className="text-sm font-bold text-gray-700 flex items-center gap-2">
                <Clock size={16} className="text-gray-400" />
                <span>
                  Thời gian
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
                  onChange={setStartTime}
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
              className="flex-1 px-4 py-2.5 text-white rounded-xl font-bold text-sm shadow-md transition-all active:scale-95 bg-linear-to-r from-blue-500 to-cyan-500 hover:shadow-lg hover:shadow-blue-500/30"
            >
              Lưu thay đổi
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default EditTimeModal;
