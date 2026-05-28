import React from "react";
import { AlertCircle, Trash2 } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Custom Confirmation dialog for deleting a comment.
 */
const DeleteCommentDialog = ({ isOpen, onClose, onConfirm, isDeleting }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
      />

      {/* Dialog */}
      <div className="relative bg-white rounded-3xl shadow-2xl w-full max-w-sm p-6 text-center transform transition-all animate-in fade-in zoom-in duration-300">
        <div className="w-14 h-14 mx-auto mb-4 rounded-full bg-red-100 flex items-center justify-center">
          <AlertCircle size={28} className="text-red-500" />
        </div>

        <h3 className="text-lg font-black text-gray-900 mb-2">
          Xóa bình luận?
        </h3>
        <p className="text-sm text-gray-500 mb-6">
          Hành động này không thể hoàn tác. Bạn có chắc muốn xóa bình luận này
          không?
        </p>

        <div className="flex items-center gap-3">
          <button
            onClick={onClose}
            disabled={isDeleting}
            className="flex-1 px-4 py-3 border border-gray-200 text-gray-600 font-bold rounded-xl hover:bg-gray-50 transition-colors disabled:opacity-50 cursor-pointer"
          >
            Hủy
          </button>
          <button
            onClick={onConfirm}
            disabled={isDeleting}
            className="flex-1 px-4 py-3 bg-red-500 text-white font-bold rounded-xl hover:bg-red-600 transition-colors disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer shadow-lg shadow-red-200"
          >
            {isDeleting ? (
              <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
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

DeleteCommentDialog.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  onConfirm: PropTypes.func.isRequired,
  isDeleting: PropTypes.bool,
};

export default DeleteCommentDialog;
