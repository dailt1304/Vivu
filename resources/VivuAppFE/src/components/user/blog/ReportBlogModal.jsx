import React, { useState, useCallback } from "react";
import { createPortal } from "react-dom";
import {
  X,
  AlertTriangle,
  ShieldCheck,
  Ban,
  BookX,
  Copyright,
  HelpCircle,
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import { useReportBlog } from "../../../hooks/blogs/useBlogs";
import toast from "../../../utils/toast";

const REPORT_TYPES = [
  { value: 1, label: "Spam / Quảng cáo", icon: Ban },
  { value: 2, label: "Nội dung không phù hợp", icon: AlertTriangle },
  { value: 3, label: "Thông tin sai lệch", icon: BookX },
  { value: 4, label: "Vi phạm bản quyền", icon: Copyright },
  { value: 5, label: "Khác", icon: HelpCircle },
];

const ReportBlogModal = ({ isOpen, onClose, blogId, blogTitle }) => {
  const [reportType, setReportType] = useState(null);
  const [reason, setReason] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");

  const { trigger: reportBlog, isMutating } = useReportBlog();

  const handleSubmit = useCallback(
    async (e) => {
      e.preventDefault();
      if (!reportType) {
        setError("Vui lòng chọn loại báo cáo.");
        return;
      }
      if (!reason.trim()) {
        setError("Vui lòng nhập lý do báo cáo.");
        return;
      }

      try {
        await reportBlog({
          blogId,
          data: {
            reportType,
            reason,
            description: description || undefined,
          },
        });
        toast.success("Báo cáo đã được gửi thành công!");
        onClose();
        // Reset form
        setReportType(null);
        setReason("");
        setDescription("");
        setError("");
      } catch (err) {
        const msg =
          err?.response?.data?.message ||
          "Đã có lỗi xảy ra. Vui lòng thử lại.";
        setError(msg);
        toast.error(msg);
      }
    },
    [reportType, reason, description, blogId, reportBlog, onClose],
  );

  const handleClose = useCallback(() => {
    setError("");
    onClose();
  }, [onClose]);

  if (!isOpen) return null;

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <motion.div
          className="fixed inset-0 z-[12000] flex items-center justify-center p-4"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          {/* Backdrop */}
          <motion.div
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            onClick={handleClose}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          />

          {/* Modal */}
          <motion.div
            className="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] flex flex-col overflow-hidden"
            initial={{ scale: 0.95, opacity: 0, y: 20 }}
            animate={{ scale: 1, opacity: 1, y: 0 }}
            exit={{ scale: 0.95, opacity: 0, y: 20 }}
            transition={{ type: "spring", duration: 0.4 }}
          >
            {/* Header */}
            <div className="shrink-0 z-10 bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-red-50 flex items-center justify-center">
                  <AlertTriangle className="w-5 h-5 text-red-500" />
                </div>
                <div>
                  <h2 className="text-lg font-semibold text-gray-900">
                    Báo cáo bài viết
                  </h2>
                  <p className="text-sm text-gray-500 truncate max-w-[250px]">
                    {blogTitle}
                  </p>
                </div>
              </div>
              <button
                onClick={handleClose}
                className="p-2 rounded-full hover:bg-gray-100 transition-colors"
              >
                <X className="w-5 h-5 text-gray-400" />
              </button>
            </div>

            {/* Body */}
            <form onSubmit={handleSubmit} className="flex flex-col min-h-0 flex-1 overflow-hidden">
              <div className="flex-1 overflow-y-auto p-6 space-y-6">
                {/* Report Type Selection */}
              <div>
                <label className="text-sm font-medium text-gray-700 mb-3 block">
                  Loại vi phạm <span className="text-red-500">*</span>
                </label>
                <div className="space-y-2">
                  {REPORT_TYPES.map((type) => {
                    const Icon = type.icon;
                    const isSelected = reportType === type.value;
                    return (
                      <button
                        key={type.value}
                        type="button"
                        onClick={() => {
                          setReportType(type.value);
                          setError("");
                        }}
                        className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl border-2 transition-all duration-200 text-left ${
                          isSelected
                            ? "border-red-300 bg-red-50 text-red-700"
                            : "border-gray-100 hover:border-gray-200 hover:bg-gray-50 text-gray-700"
                        }`}
                      >
                        <Icon
                          className={`w-5 h-5 shrink-0 ${isSelected ? "text-red-500" : "text-gray-400"}`}
                        />
                        <span className="text-sm font-medium">
                          {type.label}
                        </span>
                        {isSelected && (
                          <ShieldCheck className="w-4 h-4 text-red-500 ml-auto" />
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Reason */}
              <div>
                <label
                  htmlFor="report-reason"
                  className="text-sm font-medium text-gray-700 mb-2 block"
                >
                  Lý do <span className="text-red-500">*</span>
                </label>
                <textarea
                  id="report-reason"
                  value={reason}
                  onChange={(e) => {
                    setReason(e.target.value);
                    setError("");
                  }}
                  placeholder="Mô tả ngắn gọn lý do báo cáo..."
                  rows={2}
                  className="w-full px-4 py-3 border border-gray-200 rounded-xl text-sm focus:ring-2 focus:ring-red-200 focus:border-red-400 transition-all resize-none"
                />
              </div>

              {/* Description (optional) */}
              <div>
                <label
                  htmlFor="report-desc"
                  className="text-sm font-medium text-gray-700 mb-2 block"
                >
                  Mô tả chi tiết{" "}
                  <span className="text-gray-400">(không bắt buộc)</span>
                </label>
                <textarea
                  id="report-desc"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Cung cấp thêm thông tin chi tiết..."
                  rows={3}
                  className="w-full px-4 py-3 border border-gray-200 rounded-xl text-sm focus:ring-2 focus:ring-red-200 focus:border-red-400 transition-all resize-none"
                />
              </div>

              {/* Error */}
              {error && (
                <div className="flex items-center gap-2 text-sm text-red-600 bg-red-50 px-4 py-3 rounded-xl">
                  <AlertTriangle className="w-4 h-4 shrink-0" />
                  {error}
                </div>
              )}

              </div>

              {/* Actions */}
              <div className="shrink-0 bg-white border-t border-gray-100 px-6 py-4">
                <div className="flex gap-3">
                  <button
                    type="button"
                    onClick={handleClose}
                    className="flex-1 px-4 py-3 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-xl transition-colors"
                  >
                    Hủy
                  </button>
                  <button
                    type="submit"
                    disabled={isMutating}
                    className="flex-1 px-4 py-3 text-sm font-medium text-white bg-red-500 hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed rounded-xl transition-colors flex items-center justify-center gap-2"
                  >
                    {isMutating ? (
                      <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    ) : (
                      <AlertTriangle className="w-4 h-4" />
                    )}
                    Gửi báo cáo
                  </button>
                </div>
              </div>
            </form>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>,
    document.body,
  );
};

export default ReportBlogModal;
