import React, { useState, useRef } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, FolderPlus, Image as ImageIcon, UploadCloud } from "lucide-react";
import toast from "../../../utils/toast";

const CreateCollectionModal = ({
  isOpen,
  onClose,
  onConfirm,
  isLoading,
  initialData = null,
}) => {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [coverImage, setCoverImage] = useState(null);
  const [previewImage, setPreviewImage] = useState(null);
  const fileInputRef = useRef(null);

  React.useEffect(() => {
    if (isOpen) {
      if (initialData) {
        setName(initialData.name || initialData.title || "");
        setDescription(initialData.description || "");
        setPreviewImage(initialData.coverImageUrl || initialData.coverImage || null);
        setCoverImage(null);
      } else {
        setName("");
        setDescription("");
        setCoverImage(null);
        setPreviewImage(null);
      }
    }
  }, [isOpen, initialData]);

  const handleConfirm = () => {
    if (!name.trim()) {
      toast.error("Vui lòng nhập tên bộ sưu tập");
      return;
    }

    const formData = new FormData();
    formData.append("name", name.trim());
    if (description.trim()) {
      formData.append("description", description.trim());
    }
    // Only append if it's a new File object
    if (coverImage instanceof File) {
      formData.append("coverImage", coverImage);
    }

    onConfirm(formData, initialData?.id);
  };

  const handleImageChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        toast.error("Ảnh không được vượt quá 5MB");
        return;
      }
      setCoverImage(file);
      setPreviewImage(URL.createObjectURL(file));
    }
  };

  const isEditing = !!initialData;

  return (
    <AnimatePresence>
      {isOpen && (
        <>
        <div className="fixed top-14 lg:top-16 inset-x-0 bottom-0 flex items-center justify-center p-4 sm:p-6 z-[9000]">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={!isLoading ? onClose : undefined}
            className="absolute inset-0 bg-black/40 backdrop-blur-sm"
          />

          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", stiffness: 300, damping: 30 }}
            className="relative w-full max-w-md max-h-[90vh] flex flex-col bg-white rounded-2xl shadow-2xl overflow-hidden"
          >
            <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
              <h3 className="font-bold text-lg text-slate-800 flex items-center gap-2">
                <FolderPlus className="text-blue-500" size={20} />
                {isEditing ? "Chỉnh sửa bộ sưu tập" : "Tạo bộ sưu tập mới"}
              </h3>
              <button
                onClick={!isLoading ? onClose : undefined}
                disabled={isLoading}
                className="p-2 hover:bg-slate-50 text-slate-400 hover:text-slate-600 rounded-full transition-colors disabled:opacity-50"
              >
                <X size={20} />
              </button>
            </div>

            <div className="p-6 space-y-5 flex-1 overflow-y-auto custom-scrollbar">
              {/* Cover Image Upload */}
              <div>
                <label className="block text-sm font-bold text-slate-700 mb-1.5">
                  Ảnh bìa (Tùy chọn)
                </label>
                <div 
                  onClick={() => fileInputRef.current?.click()}
                  className="relative aspect-video bg-slate-50 rounded-xl border-2 border-dashed border-slate-200 flex flex-col items-center justify-center text-slate-400 gap-2 cursor-pointer hover:bg-blue-50 hover:border-blue-200 hover:text-blue-500 transition-all group overflow-hidden"
                >
                  {previewImage ? (
                    <>
                      <img src={previewImage} alt="Cover Preview" className="w-full h-full object-cover" />
                      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center text-white">
                        <UploadCloud size={24} />
                        <span className="text-sm font-medium mt-1">Đổi ảnh khác</span>
                      </div>
                    </>
                  ) : (
                    <>
                      <ImageIcon size={28} className="group-hover:scale-110 transition-transform text-slate-300 group-hover:text-blue-400" />
                      <span className="text-sm font-medium text-slate-500 group-hover:text-blue-600">
                        Nhấn để tải ảnh lên
                      </span>
                    </>
                  )}
                  <input 
                    type="file" 
                    ref={fileInputRef} 
                    onChange={handleImageChange} 
                    accept="image/jpeg,image/png,image/webp"
                    className="hidden" 
                  />
                </div>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-bold text-slate-700 mb-1.5">
                    Tên bộ sưu tập <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="Ví dụ: Chuyến đi mùa hè, Quán cà phê..."
                    className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none font-medium transition-all"
                    autoFocus
                  />
                </div>
                
                <div>
                  <label className="block text-sm font-bold text-slate-700 mb-1.5">
                    Mô tả (Tùy chọn)
                  </label>
                  <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Viết vài dòng mô tả..."
                    rows={3}
                    className="w-full px-4 py-3 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 outline-none font-medium transition-all resize-none custom-scrollbar"
                  />
                </div>
              </div>
            </div>

            <div className="p-4 bg-slate-50 flex justify-end gap-3 border-t border-slate-100">
              <button
                onClick={onClose}
                disabled={isLoading}
                className="px-5 py-2.5 rounded-xl font-bold text-sm text-slate-600 hover:bg-white hover:shadow-sm transition-all disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                onClick={handleConfirm}
                disabled={isLoading}
                className="px-6 py-2.5 bg-gradient-primary text-white rounded-xl font-bold text-sm shadow-md shadow-blue-200 hover:shadow-lg hover:shadow-blue-300 hover:bg-blue-600 active:scale-95 transition-all disabled:opacity-50 flex items-center gap-2"
              >
                {isLoading && (
                  <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                )}
                {isEditing ? "Lưu thay đổi" : "Tạo bộ sưu tập"}
              </button>
            </div>
          </motion.div>
        </div>
        </>
      )}
    </AnimatePresence>
  );
};

export default CreateCollectionModal;
