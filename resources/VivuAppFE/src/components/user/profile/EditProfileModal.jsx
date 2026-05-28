import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Camera, FileText, Loader2, Link as LinkIcon } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Edit profile modal - simplified version
 * Only allows editing cover/background and bio
 */
const EditProfileModal = ({ isOpen, onClose, user, onSave }) => {
  const [formData, setFormData] = useState({
    bio: user?.bio || "",
    cover: user?.cover || "",
  });
  const [coverInputMode, setCoverInputMode] = useState("upload"); // 'upload' or 'url'
  const [coverUrl, setCoverUrl] = useState("");
  const [loading, setLoading] = useState(false);

  const handleChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleCoverUpload = (e) => {
    const file = e.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        handleChange("cover", reader.result);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleCoverUrlSubmit = () => {
    if (coverUrl.trim()) {
      handleChange("cover", coverUrl.trim());
      setCoverUrl("");
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await onSave?.(formData);
      onClose();
    } catch (error) {
      console.error("Failed to save profile:", error);
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        {/* Backdrop */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        />

        {/* Modal */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden z-10"
        >
          {/* Header */}
          <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <h2 className="text-xl font-bold text-gray-900">Chỉnh sửa hồ sơ</h2>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X size={20} className="text-gray-500" />
            </button>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit}>
            <div className="p-6 space-y-6">
              {/* Cover Photo Section */}
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-3">
                  Ảnh bìa
                </label>

                {/* Current Cover Preview */}
                <div className="relative h-32 rounded-xl overflow-hidden mb-3 bg-gray-100">
                  {formData.cover ? (
                    <img
                      src={formData.cover}
                      alt="Cover preview"
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full bg-gradient-to-br from-blue-400 to-indigo-600 flex items-center justify-center">
                      <Camera size={32} className="text-white/50" />
                    </div>
                  )}
                </div>

                {/* Input Mode Tabs */}
                <div className="flex gap-2 mb-3">
                  <button
                    type="button"
                    onClick={() => setCoverInputMode("upload")}
                    className={`flex-1 py-2 px-4 text-sm font-medium rounded-lg transition-all ${
                      coverInputMode === "upload"
                        ? "bg-blue-100 text-blue-700"
                        : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                    }`}
                  >
                    <Camera size={16} className="inline mr-1.5" />
                    Tải lên
                  </button>
                  <button
                    type="button"
                    onClick={() => setCoverInputMode("url")}
                    className={`flex-1 py-2 px-4 text-sm font-medium rounded-lg transition-all ${
                      coverInputMode === "url"
                        ? "bg-blue-100 text-blue-700"
                        : "bg-gray-100 text-gray-600 hover:bg-gray-200"
                    }`}
                  >
                    <LinkIcon size={16} className="inline mr-1.5" />
                    URL
                  </button>
                </div>

                {/* Upload Input */}
                {coverInputMode === "upload" && (
                  <label className="flex items-center justify-center w-full py-4 border-2 border-dashed border-gray-200 rounded-xl cursor-pointer hover:border-blue-400 hover:bg-blue-50/50 transition-all">
                    <div className="text-center">
                      <Camera
                        size={24}
                        className="mx-auto text-gray-400 mb-2"
                      />
                      <span className="text-sm text-gray-600">
                        Nhấn để chọn ảnh
                      </span>
                    </div>
                    <input
                      type="file"
                      accept="image/*"
                      onChange={handleCoverUpload}
                      className="hidden"
                    />
                  </label>
                )}

                {/* URL Input */}
                {coverInputMode === "url" && (
                  <div className="flex gap-2">
                    <input
                      type="url"
                      value={coverUrl}
                      onChange={(e) => setCoverUrl(e.target.value)}
                      placeholder="Nhập URL hình ảnh..."
                      className="flex-1 px-4 py-3 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-100 focus:border-blue-500"
                    />
                    <button
                      type="button"
                      onClick={handleCoverUrlSubmit}
                      disabled={!coverUrl.trim()}
                      className="px-4 py-3 bg-blue-500 text-white font-medium rounded-xl hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      Áp dụng
                    </button>
                  </div>
                )}
              </div>

              {/* Bio Section */}
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-2">
                  Giới thiệu bản thân
                </label>
                <div className="relative">
                  <textarea
                    value={formData.bio}
                    onChange={(e) => handleChange("bio", e.target.value)}
                    placeholder="Viết vài dòng về bạn..."
                    rows={4}
                    maxLength={200}
                    className="w-full px-4 py-3 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-100 focus:border-blue-500 transition-all resize-none"
                  />
                  <span className="absolute right-3 bottom-3 text-xs text-gray-400">
                    {formData.bio.length}/200
                  </span>
                </div>
              </div>

              {/* Settings Link */}
              <div className="p-4 bg-gray-50 rounded-xl">
                <p className="text-sm text-gray-500">
                  Để thay đổi ảnh đại diện, tên, email và các thông tin khác,
                  vui lòng vào{" "}
                  <a
                    href="/profile"
                    className="text-blue-600 font-medium hover:underline"
                  >
                    Cài đặt
                  </a>
                </p>
              </div>
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-gray-100 flex gap-3 justify-end">
              <button
                type="button"
                onClick={onClose}
                className="px-5 py-2.5 text-gray-600 font-medium hover:bg-gray-100 rounded-xl transition-colors"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={loading}
                className="px-5 py-2.5 bg-gradient-to-r from-blue-500 to-indigo-600 text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {loading && <Loader2 size={16} className="animate-spin" />}
                Lưu thay đổi
              </button>
            </div>
          </form>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

EditProfileModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  user: PropTypes.object,
  onSave: PropTypes.func,
};

export default EditProfileModal;
