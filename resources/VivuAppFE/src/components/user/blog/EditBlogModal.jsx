import React, { useState, useCallback } from "react";
import { X, Upload, ImageIcon } from "lucide-react";

/**
 * Edit Blog Modal — basic fields: Title, ShortDescription, CoverImage.
 * Vercel Rules: rerender-move-effect-to-event, rerender-functional-setstate,
 *               rendering-conditional-render, js-early-exit
 */
const EditBlogModal = ({ isOpen, blog, onClose, onSubmit, isSubmitting }) => {
  // Vercel Rule: js-early-exit
  if (!isOpen || !blog) return null;

  return (
    <EditBlogForm
      blog={blog}
      onClose={onClose}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
    />
  );
};

// Separated to allow hooks usage (useState) after early exit guard
function EditBlogForm({ blog, onClose, onSubmit, isSubmitting }) {
  const [title, setTitle] = useState(blog.title || "");
  const [shortDescription, setShortDescription] = useState(
    blog.shortDescription || "",
  );
  const [coverFile, setCoverFile] = useState(null);
  const [coverPreview, setCoverPreview] = useState(blog.coverImageUrl || null);

  // Vercel Rule: rerender-move-effect-to-event — logic in event handler
  const handleFileChange = useCallback((e) => {
    const file = e.target.files?.[0];
    if (file) {
      setCoverFile(file);
      setCoverPreview(URL.createObjectURL(file));
    }
  }, []);

  // Vercel Rule: rerender-move-effect-to-event
  const handleSubmit = useCallback(
    (e) => {
      e.preventDefault();
      if (!title.trim()) return;

      const formData = new FormData();
      formData.append("Title", title.trim());
      formData.append("ShortDescription", shortDescription.trim());
      if (coverFile) {
        formData.append("CoverImage", coverFile);
      }

      onSubmit?.({ blogId: blog.id, formData });
    },
    [title, shortDescription, coverFile, blog.id, onSubmit],
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="relative bg-white rounded-3xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-white z-10 flex items-center justify-between px-6 py-4 border-b border-gray-100 rounded-t-3xl">
          <h2 className="text-xl font-black text-gray-900">
            Chỉnh sửa bài viết
          </h2>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-100 rounded-xl transition-colors cursor-pointer"
          >
            <X size={20} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          {/* Title */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-2">
              Tiêu đề <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              placeholder="Nhập tiêu đề bài viết..."
              className="w-full px-4 py-3 border border-gray-200 rounded-xl text-sm font-medium focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-colors"
            />
            <div className="text-right text-xs text-gray-400 mt-1">
              {title.length}/200
            </div>
          </div>

          {/* Short Description */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-2">
              Mô tả ngắn
            </label>
            <textarea
              value={shortDescription}
              onChange={(e) => setShortDescription(e.target.value)}
              maxLength={500}
              rows={3}
              placeholder="Viết mô tả ngắn cho bài viết..."
              className="w-full px-4 py-3 border border-gray-200 rounded-xl text-sm font-medium focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-colors resize-none"
            />
            <div className="text-right text-xs text-gray-400 mt-1">
              {shortDescription.length}/500
            </div>
          </div>

          {/* Cover Image */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-2">
              Ảnh bìa
            </label>
            {/* Vercel Rule: rendering-conditional-render — ternary */}
            {coverPreview ? (
              <div className="relative rounded-xl overflow-hidden aspect-video bg-gray-100 mb-2">
                <img
                  src={coverPreview}
                  alt="Preview"
                  className="w-full h-full object-cover"
                />
                <button
                  type="button"
                  onClick={() => {
                    setCoverFile(null);
                    setCoverPreview(null);
                  }}
                  className="absolute top-2 right-2 p-1.5 bg-black/50 rounded-full text-white hover:bg-black/70 transition-colors cursor-pointer"
                >
                  <X size={14} />
                </button>
              </div>
            ) : (
              <label className="flex flex-col items-center justify-center w-full aspect-video border-2 border-dashed border-gray-200 rounded-xl cursor-pointer hover:border-blue-400 hover:bg-blue-50/30 transition-colors">
                <ImageIcon size={32} className="text-gray-300 mb-2" />
                <span className="text-sm text-gray-400 font-medium">
                  Nhấn để chọn ảnh bìa
                </span>
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={handleFileChange}
                />
              </label>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-3 border border-gray-200 text-gray-600 font-bold rounded-xl hover:bg-gray-50 transition-colors cursor-pointer"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={!title.trim() || isSubmitting}
              className="flex-1 px-4 py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-md shadow-blue-200 hover:shadow-lg transition-all disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
            >
              {isSubmitting ? (
                <>
                  <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Đang lưu...
                </>
              ) : (
                <>
                  <Upload size={16} />
                  Lưu thay đổi
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default EditBlogModal;
