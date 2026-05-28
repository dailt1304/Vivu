import React from "react";
import { PenSquare, Clock, FileText, ArrowRight, X } from "lucide-react";
// eslint-disable-next-line no-unused-vars
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";

/**
 * Dialog to let user pick an existing draft or start a new blog
 */
const DraftPickerDialog = ({ isOpen, drafts, onClose }) => {
  const navigate = useNavigate();

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="absolute inset-0 bg-zinc-950/40 backdrop-blur-sm"
          onClick={onClose}
        />

        {/* Dialog */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 10 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 10 }}
          className="relative bg-white rounded-[2rem] shadow-[0_20px_60px_-15px_rgba(0,0,0,0.15)] w-full max-w-md p-6 max-h-[85vh] flex flex-col"
        >
          {/* Header */}
          <div className="flex items-center justify-between mb-6 shrink-0">
            <div>
              <h3 className="text-xl font-black text-gray-900 tracking-tight flex items-center gap-2">
                <FileText size={22} className="text-blue-500" />
                Tiếp tục viết?
              </h3>
              <p className="text-sm text-gray-500 font-medium mt-1">
                Bạn có {drafts?.length || 0} bản nháp chưa hoàn thành
              </p>
            </div>
            <button
              onClick={onClose}
              className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X size={20} />
            </button>
          </div>

          {/* Drafts List */}
          <div className="flex-1 overflow-y-auto space-y-3 mb-6 pr-1 custom-scrollbar">
            {drafts?.map((draft, idx) => (
              <motion.button
                key={draft.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.05 }}
                onClick={() => {
                  onClose();
                  navigate(`/inspiration/edit/${draft.id}`);
                }}
                className="w-full text-left p-4 rounded-2xl border border-slate-200/60 bg-white hover:bg-blue-50/50 hover:border-blue-200 hover:shadow-lg hover:shadow-blue-500/5 transition-all group"
              >
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1 min-w-0">
                    <h4 className="font-bold text-gray-900 truncate">
                      {draft.title || "Chưa có tiêu đề"}
                    </h4>
                    <div className="flex items-center gap-3 mt-1.5 text-xs font-medium text-gray-500">
                      <span className="flex items-center gap-1">
                        <Clock size={12} />
                        {new Date(
                          draft.createdAt || draft.savedAt
                        ).toLocaleDateString("vi-VN", {
                          day: "2-digit",
                          month: "2-digit",
                          year: "numeric",
                        })}
                      </span>
                      {draft.tripId && (
                        <span className="px-1.5 py-0.5 bg-gray-100 rounded-md text-gray-600">
                          Từ một chuyến đi
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-500 group-hover:bg-blue-500 group-hover:text-white transition-colors shrink-0">
                    <ArrowRight size={16} />
                  </div>
                </div>
              </motion.button>
            ))}
          </div>

          {/* Start New Button */}
          <div className="shrink-0 pt-4 border-t border-gray-100">
            <button
              onClick={() => {
                onClose();
                navigate("/inspiration/create");
              }}
              className="w-full flex items-center justify-center gap-2 py-3.5 text-blue-600 font-bold rounded-xl hover:bg-blue-50 transition-colors"
            >
              <PenSquare size={18} />
              Viết bài mới hoàn toàn
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default DraftPickerDialog;
