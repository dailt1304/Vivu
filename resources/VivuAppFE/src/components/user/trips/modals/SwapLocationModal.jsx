import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, MapPin, Clock } from "lucide-react";

const SwapLocationModal = ({
  isOpen,
  onClose,
  onConfirm,
  items,
  dayIds,
  getDateLabel,
  ideaTitle,
}) => {
  const [selectedTarget, setSelectedTarget] = useState(null);

  useEffect(() => {
    if (!isOpen) {
      // Clear data when modal closes to reset state, avoiding synchronous setState when opening
      setTimeout(() => setSelectedTarget(null), 300);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  // Collect all schedule items (day-1, day-2, ...) — exclude ideas
  const scheduleEntries = dayIds
    .map((dayId, index) => ({
      dayId,
      dayLabel: `Ngày ${index + 1} - ${getDateLabel(index)}`,
      locations: items[dayId] || [],
    }))
    .filter((entry) => entry.locations.length > 0);

  const totalLocations = scheduleEntries.reduce(
    (acc, curr) => acc + curr.locations.length,
    0
  );

  const handleConfirm = () => {
    if (selectedTarget) {
      onConfirm(selectedTarget.item, selectedTarget.dayId);
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
          className="relative bg-white rounded-2xl shadow-xl w-full max-w-md z-10 flex flex-col max-h-[85vh]"
        >
          {/* Header */}
          <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between shrink-0">
            <h3 className="font-bold text-lg text-slate-800">
              Thay thế địa điểm
            </h3>
            <button
              onClick={onClose}
              className="p-2 -mr-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-full transition-colors"
            >
              <X size={20} />
            </button>
          </div>

          <div className="p-6 space-y-4 overflow-y-auto min-h-0 flex-1">
            {/* Idea Info Preview */}
            <div className="flex items-start gap-3 p-3 bg-blue-50/50 rounded-xl border border-blue-100/50 shrink-0">
              <div className="mt-0.5 min-w-[20px]">
                <MapPin size={18} className="text-blue-600" />
              </div>
              <div>
                <h4 className="font-bold text-gray-800 text-sm line-clamp-1">
                  {ideaTitle}
                </h4>
                <p className="text-xs text-blue-600/80 mt-0.5 font-medium leading-relaxed">
                  Ý tưởng này sẽ thay thế địa điểm bạn chọn bên dưới. Địa điểm
                  cũ sẽ bị xóa khỏi lịch trình.
                </p>
              </div>
            </div>

            {totalLocations === 0 ? (
              <div className="text-center py-8">
                <p className="text-sm text-slate-500 font-medium">
                  Chưa có địa điểm nào trong lịch trình.
                </p>
              </div>
            ) : (
              <div className="space-y-4">
                <label className="text-sm font-bold text-slate-700 block">
                  Chọn địa điểm để thay thế
                </label>
                
                <div className="space-y-5">
                  {scheduleEntries.map(({ dayId, dayLabel, locations }) => (
                    <div key={dayId} className="space-y-2">
                      <h5 className="text-xs font-bold text-slate-500 uppercase tracking-wider pl-1">
                        {dayLabel}
                      </h5>
                      <div className="grid grid-cols-1 gap-2">
                        {locations.map((loc) => {
                          const isSelected =
                            selectedTarget?.item?.id === loc.id;
                          return (
                            <button
                              key={loc.id}
                              onClick={() =>
                                setSelectedTarget({ item: loc, dayId })
                              }
                              className={`group flex items-center gap-3 p-2 rounded-xl border transition-all duration-200 text-left ${
                                isSelected
                                  ? "border-transparent bg-blue-50 shadow-sm ring-1 ring-blue-500"
                                  : "border-gray-100 hover:border-blue-200 hover:bg-gray-50"
                              }`}
                            >
                              <img
                                src={loc.image || "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop"}
                                alt={loc.content}
                                className="w-12 h-12 rounded-lg object-cover bg-gray-100 shrink-0"
                                onError={(e) => {
                                  e.target.onerror = null;
                                  e.target.src = "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop";
                                }}
                              />
                              <div className="flex-1 min-w-0">
                                <h6
                                  className={`font-semibold text-sm truncate ${
                                    isSelected
                                      ? "text-blue-700"
                                      : "text-slate-700"
                                  }`}
                                >
                                  {loc.content}
                                </h6>
                                <div className="flex items-center gap-1.5 mt-0.5 text-xs text-slate-500">
                                  <Clock size={12} />
                                  <span>
                                    {loc.startTime || "--:--"} -{" "}
                                    {loc.endTime || "--:--"}
                                  </span>
                                </div>
                              </div>
                              <div
                                className={`w-5 h-5 rounded-full border flex items-center justify-center shrink-0 mr-1 transition-colors ${
                                  isSelected
                                    ? "border-transparent bg-blue-500 text-white"
                                    : "border-slate-300 bg-white"
                                }`}
                              >
                                {isSelected && (
                                  <motion.div
                                    initial={{ scale: 0 }}
                                    animate={{ scale: 1 }}
                                    className="w-2 h-2 bg-white rounded-full"
                                  />
                                )}
                              </div>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="p-4 border-t border-slate-100 flex gap-3 bg-slate-50/50 rounded-b-2xl shrink-0">
            <button
              onClick={onClose}
              className="flex-1 px-4 py-2.5 bg-white border border-slate-200 text-slate-700 rounded-xl font-bold text-sm hover:bg-slate-50 hover:border-slate-300 transition-colors shadow-[0_1px_2px_rgba(0,0,0,0.05)]"
            >
              Hủy
            </button>
            <button
              onClick={handleConfirm}
              disabled={!selectedTarget}
              className={`flex-1 px-4 py-2.5 text-white rounded-xl font-bold text-sm shadow-md transition-all active:scale-[0.98] ${
                selectedTarget
                  ? "bg-linear-to-r from-blue-500 to-cyan-500 hover:shadow-lg hover:shadow-blue-500/30"
                  : "bg-slate-300 cursor-not-allowed shadow-none"
              }`}
            >
              Xác nhận thay thế
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default React.memo(SwapLocationModal);
