import React, { useState, useCallback } from "react";
import { createPortal } from "react-dom";
import { X, AlertTriangle, ShieldCheck, Info, Store, HelpCircle, UploadCloud, Image as ImageIcon, XCircle } from "lucide-react";
// eslint-disable-next-line no-unused-vars
import { AnimatePresence, motion } from "framer-motion";
import { useReportLocation } from "../../../hooks/locations/useLocations";
import toast from "../../../utils/toast";
import AddressAutocomplete from "./AddressAutocomplete";

const ICON_MAP = { info: Info, store: Store, help: HelpCircle };

const REPORT_TYPES = [
  { value: 1, label: "Thông tin không chính xác", icon: "info" },
  { value: 2, label: "Địa điểm đã đóng cửa", icon: "store" },
  { value: 4, label: "Khác", icon: "help" },
];

const ReportLocationModal = ({ isOpen, onClose, locationId, locationName }) => {
  const [reportType, setReportType] = useState(null);
  const [reason, setReason] = useState("");
  const [description, setDescription] = useState("");
  const [suggestedName, setSuggestedName] = useState("");
  const [suggestedAddress, setSuggestedAddress] = useState("");
  const [evidenceImages, setEvidenceImages] = useState([]);
  const [error, setError] = useState("");

  const { trigger: reportLocation, isMutating } = useReportLocation();

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
        const formData = new FormData();
        formData.append("locationId", locationId);
        formData.append("reportType", reportType);
        formData.append("reason", reason);
        if (description) formData.append("description", description);
        if (suggestedName) formData.append("suggestedName", suggestedName);
        if (suggestedAddress) formData.append("suggestedAddress", suggestedAddress);
        
        evidenceImages.forEach((file) => {
          formData.append("evidenceImages", file);
        });

        await reportLocation(formData);
        toast.success("Báo cáo đã được gửi thành công!");
        onClose();
      } catch (err) {
        const msg =
          err?.response?.data?.message || "Đã có lỗi xảy ra. Vui lòng thử lại.";
        setError(msg);
        toast.error(msg);
      }
    },
    [reportType, reason, description, suggestedName, suggestedAddress, evidenceImages, locationId, reportLocation, onClose],
  );

  const handleImageChange = (e) => {
    const files = Array.from(e.target.files);
    
    // Check total limit
    if (evidenceImages.length + files.length > 3) {
      toast.error("Bạn chỉ có thể tải lên tối đa 3 ảnh.");
      return;
    }

    const validFiles = [];
    for (const file of files) {
      if (!file.type.startsWith("image/")) {
        toast.error(`${file.name} không phải là ảnh hợp lệ.`);
        continue;
      }
      if (file.size > 5 * 1024 * 1024) {
        toast.error(`${file.name} vượt quá dung lượng 5MB.`);
        continue;
      }
      validFiles.push(file);
    }

    if (validFiles.length > 0) {
      setEvidenceImages((prev) => [...prev, ...validFiles]);
    }
    
    // Reset file input
    e.target.value = "";
  };

  const removeImage = (index) => {
    setEvidenceImages((prev) => prev.filter((_, i) => i !== index));
  };

  if (!isOpen) return null;

  return createPortal(
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="fixed inset-0 bg-black/50 backdrop-blur-sm z-[9999] flex items-start justify-center p-4 sm:p-6 pt-24 sm:pt-28"
      >
        <motion.div
          initial={{ scale: 0.95, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          exit={{ scale: 0.95, opacity: 0 }}
          onClick={(e) => e.stopPropagation()}
          className="bg-white dark:bg-gray-800 rounded-2xl w-full max-w-md overflow-hidden shadow-2xl relative flex flex-col max-h-[75vh]"
        >
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-100 dark:border-gray-700 shrink-0">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center text-red-500">
                <AlertTriangle size={20} />
              </div>
              <div>
                <h2 className="text-xl font-bold text-gray-900 dark:text-white">
                  Báo cáo địa điểm
                </h2>
                <p className="text-sm text-gray-500 line-clamp-1">
                  {locationName}
                </p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-full transition-colors"
              disabled={isMutating}
            >
              <X size={20} />
            </button>
          </div>

          {/* Body */}
          <div className="p-6 overflow-y-auto flex-1 min-h-0 custom-scrollbar">
            <form
              id="report-form"
              onSubmit={handleSubmit}
              className="space-y-6"
            >
              {error ? (
                <div className="p-3 bg-red-50 text-red-600 rounded-lg text-sm flex items-start gap-2">
                  <AlertTriangle size={16} className="mt-0.5 shrink-0" />
                  <span>{error}</span>
                </div>
              ) : null}

              <div className="space-y-3">
                <label className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Loại báo cáo <span className="text-red-500">*</span>
                </label>
                <div className="space-y-2">
                  {REPORT_TYPES.map((type) => (
                    <label
                      key={type.value}
                      className={`flex items-center p-3 rounded-xl border cursor-pointer transition-colors ${
                        reportType === type.value
                          ? "border-primary bg-primary/5 text-primary"
                          : "border-gray-200 hover:border-gray-300 dark:border-gray-700 dark:hover:border-gray-600"
                      }`}
                    >
                      <input
                        type="radio"
                        name="reportType"
                        value={type.value}
                        checked={reportType === type.value}
                        onChange={() => setReportType(type.value)}
                        className="w-4 h-4 text-primary bg-gray-100 border-gray-300 focus:ring-primary"
                      />
                      <span className="ml-3 text-sm font-medium flex items-center gap-1.5">
                        {React.createElement(ICON_MAP[type.icon], { className: "w-4 h-4" })}
                        {type.label}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Lý do <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="VD: Định vị bị sai lệch 500m"
                  className="w-full px-4 py-3 rounded-xl border border-gray-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none transition-all dark:bg-gray-800 dark:border-gray-700"
                />
              </div>

              {reportType === 1 && (
                <div className="space-y-3 bg-blue-50/50 dark:bg-blue-900/10 p-4 rounded-xl border border-blue-100 dark:border-blue-800">
                  <p className="text-xs font-semibold text-blue-700 dark:text-blue-400">
                    Đề xuất thông tin chính xác (tuỳ chọn)
                  </p>
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Tên đề xuất
                    </label>
                    <input
                      type="text"
                      value={suggestedName}
                      onChange={(e) => setSuggestedName(e.target.value)}
                      placeholder="VD: Quán Cà Phê Sương Mai"
                      className="w-full px-4 py-2.5 rounded-xl border border-gray-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none transition-all text-sm dark:bg-gray-800 dark:border-gray-700"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Địa chỉ đề xuất
                    </label>
                    <AddressAutocomplete
                      value={suggestedAddress}
                      onChange={setSuggestedAddress}
                      onSelectPlace={(address) => setSuggestedAddress(address)}
                    />
                  </div>
                </div>
              )}

              <div className="space-y-2">
                <label className="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Mô tả chi tiết (Tuỳ chọn)
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Cung cấp thêm thông tin để giúp chúng tôi xác minh nhanh hơn..."
                  rows={3}
                  className="w-full px-4 py-3 rounded-xl border border-gray-200 focus:border-primary focus:ring-1 focus:ring-primary outline-none transition-all resize-none dark:bg-gray-800 dark:border-gray-700"
                />
              </div>

              {/* Evidence Images */}
              <div className="space-y-3">
                <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 flex justify-between items-end">
                  <span>Ảnh bằng chứng (Tuỳ chọn)</span>
                  <span className="text-xs font-normal text-gray-500">
                    {evidenceImages.length}/3 ảnh
                  </span>
                </label>
                
                {/* Upload Zone */}
                {evidenceImages.length < 3 && (
                  <label className="w-full relative group cursor-pointer">
                    <input
                      type="file"
                      multiple
                      accept="image/jpeg,image/png,image/webp"
                      onChange={handleImageChange}
                      className="hidden"
                    />
                    <div className="w-full px-4 py-6 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-xl bg-gray-50 dark:bg-gray-800/50 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors flex flex-col items-center justify-center gap-2 group-hover:border-primary">
                      <div className="w-10 h-10 rounded-full bg-white dark:bg-gray-700 shadow-sm flex items-center justify-center text-primary group-hover:scale-110 transition-transform">
                        <UploadCloud size={20} />
                      </div>
                      <div className="text-center">
                        <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Nhấn để tải lên ảnh bằng chứng
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          PNG, JPG tối đa 5MB
                        </p>
                      </div>
                    </div>
                  </label>
                )}

                {/* Selected Images Preview */}
                {evidenceImages.length > 0 && (
                  <div className="grid grid-cols-3 gap-3">
                    {evidenceImages.map((file, index) => (
                      <div
                        key={index}
                        className="relative aspect-square rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700 group bg-gray-100 dark:bg-gray-800 flex items-center justify-center"
                      >
                        <img
                          src={URL.createObjectURL(file)}
                          alt={`preview ${index}`}
                          className="w-full h-full object-cover"
                          onLoad={(e) => URL.revokeObjectURL(e.target.src)}
                        />
                        <button
                          type="button"
                          onClick={() => removeImage(index)}
                          className="absolute top-1.5 right-1.5 w-6 h-6 bg-black/50 hover:bg-red-500 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-all backdrop-blur-sm"
                        >
                          <X size={14} />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </form>
          </div>

          {/* Footer */}
          <div className="p-6 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 flex items-center justify-end shrink-0">
            <div className="flex gap-3">
              <button
                type="button"
                onClick={onClose}
                disabled={isMutating}
                className="px-5 py-2.5 rounded-xl text-sm font-semibold text-gray-600 hover:bg-gray-200 transition-colors disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                type="submit"
                form="report-form"
                disabled={isMutating}
                className="px-5 py-2.5 rounded-xl text-sm font-semibold text-white bg-red-600 hover:bg-red-700 shadow-lg shadow-red-200 dark:shadow-red-900/20 transition-all active:scale-95 disabled:opacity-50 flex items-center"
              >
                {isMutating ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin mr-2" />
                    Đang gửi...
                  </>
                ) : (
                  "Gửi báo cáo"
                )}
              </button>
            </div>
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>,
    document.body,
  );
};

export default ReportLocationModal;
