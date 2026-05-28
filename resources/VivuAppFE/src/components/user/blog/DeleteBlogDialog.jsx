import React from "react";
import { AlertTriangle, Trash2 } from "lucide-react";

/**
 * Confirmation dialog for deleting a blog.
 * Vercel Rules: js-early-exit, rerender-move-effect-to-event, rendering-hoist-jsx
 */
const DeleteBlogDialog = ({ isOpen, blog, onClose, onConfirm, isDeleting }) => {
  // Vercel Rule: js-early-exit
  if (!isOpen || !blog) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Dialog */}
      <div className="relative bg-white rounded-3xl shadow-2xl w-full max-w-sm p-6 text-center">
        <div className="w-14 h-14 mx-auto mb-4 rounded-full bg-red-100 flex items-center justify-center">
          <AlertTriangle size={28} className="text-red-500" />
        </div>

        <h3 className="text-lg font-black text-gray-900 mb-2">Xóa bài viết?</h3>
        <p className="text-sm text-gray-500 mb-1">
          Bạn có chắc muốn xóa bài viết:
        </p>
        <p className="text-sm font-bold text-gray-800 mb-6 line-clamp-2">
          &ldquo;{blog.title}&rdquo;
        </p>

        <div className="flex items-center gap-3">
          <button
            onClick={onClose}
            disabled={isDeleting}
            className="flex-1 px-4 py-3 border border-gray-200 text-gray-600 font-bold rounded-xl hover:bg-gray-50 transition-colors disabled:opacity-50 cursor-pointer"
          >
            Hủy
          </button>
          {/* Vercel Rule: rerender-move-effect-to-event — delete in onClick */}
          <button
            onClick={() => onConfirm?.(blog.id)}
            disabled={isDeleting}
            className="flex-1 px-4 py-3 bg-red-500 text-white font-bold rounded-xl hover:bg-red-600 transition-colors disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
          >
            {isDeleting ? (
              <>
                <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                Đang xóa...
              </>
            ) : (
              <>
                <Trash2 size={16} />
                Xóa
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default DeleteBlogDialog;
