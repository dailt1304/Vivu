import React from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, AlertTriangle } from "lucide-react";

const ConfirmationModal = ({
  isOpen,
  onClose,
  onConfirm,
  title = "Xác nhận xóa",
  message = "Bạn có chắc chắn muốn xóa mục này không? Hành động này không thể hoàn tác.",
  confirmText = "Xóa",
  cancelText = "Hủy",
  isDanger = true,
}) => {
  if (!isOpen) return null;

  const styles = isDanger
    ? {
        iconBg: "bg-red-100/50 text-red-500 ring-red-50",
        button: "bg-red-600 hover:bg-red-700 shadow-red-200",
      }
    : {
        iconBg: "bg-blue-100/50 text-blue-500 ring-blue-50",
        button:
          "bg-linear-to-r from-blue-500 to-cyan-500 hover:shadow-lg shadow-blue-200",
      };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-90 flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="fixed inset-0 bg-black/40 backdrop-blur-sm"
        />

        {/* Modal */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden z-10"
        >
          {/* Close Button */}
          <button
            onClick={onClose}
            className="absolute top-4 right-4 p-1 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-full transition-colors"
          >
            <X className="w-5 h-5" />
          </button>

          <div className="p-6 pt-8 text-center">
            <div
              className={`w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4 ring-8 ${styles.iconBg}`}
            >
              <AlertTriangle className="w-8 h-8" />
            </div>

            <h3 className="text-xl font-bold text-gray-900 mb-2">{title}</h3>
            <p className="text-gray-500 text-sm leading-relaxed mb-8">
              {message}
            </p>

            <div className="flex items-center gap-3">
              <button
                onClick={onClose}
                className="flex-1 px-4 py-2.5 bg-white border border-gray-200 text-gray-700 rounded-xl font-medium hover:bg-gray-50 hover:border-gray-300 transition-colors"
              >
                {cancelText}
              </button>
              <button
                onClick={onConfirm}
                className={`flex-1 px-4 py-2.5 text-white rounded-xl font-medium shadow-lg transition-transform active:scale-95 ${styles.button}`}
              >
                {confirmText}
              </button>
            </div>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

export default ConfirmationModal;
