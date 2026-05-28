import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Star, Send } from "lucide-react";
import PropTypes from "prop-types";
import RatingStars from "./RatingStars";

const RatingModal = ({ isOpen, onClose, onSubmit, blogTitle }) => {
  const [selectedRating, setSelectedRating] = useState(0);
  const [review, setReview] = useState("");
  const [hoveredRating, setHoveredRating] = useState(0);

  if (!isOpen) return null;

  const handleSubmit = () => {
    if (selectedRating > 0) {
      onSubmit({ rating: selectedRating, review });
      setSelectedRating(0);
      setReview("");
      onClose();
    }
  };

  const ratingLabels = {
    1: "Tệ",
    2: "Không hay",
    3: "Bình thường",
    4: "Hay",
    5: "Tuyệt vời!",
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
          className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        />

        {/* Modal */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden z-10"
        >
          {/* Header */}
          <div className="p-5 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h3 className="text-lg font-bold text-gray-900">
                Đánh giá bài viết
              </h3>
              <p className="text-sm text-gray-500 line-clamp-1">{blogTitle}</p>
            </div>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-full transition-colors"
            >
              <X size={20} className="text-gray-500" />
            </button>
          </div>

          {/* Body */}
          <div className="p-6 space-y-6">
            {/* Star Selection */}
            <div className="text-center">
              <div className="flex justify-center gap-2 mb-3">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => setSelectedRating(star)}
                    onMouseEnter={() => setHoveredRating(star)}
                    onMouseLeave={() => setHoveredRating(0)}
                    className="p-1 transition-transform hover:scale-125 focus:outline-none"
                  >
                    <Star
                      size={36}
                      className={`transition-all ${
                        star <= (hoveredRating || selectedRating)
                          ? "text-amber-400 fill-amber-400 drop-shadow-lg"
                          : "text-gray-300"
                      }`}
                    />
                  </button>
                ))}
              </div>
              <p className="text-sm font-medium text-gray-600 h-5">
                {ratingLabels[hoveredRating || selectedRating] || "Chọn số sao"}
              </p>
            </div>

            {/* Review Text */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Nhận xét (tùy chọn)
              </label>
              <textarea
                value={review}
                onChange={(e) => setReview(e.target.value)}
                placeholder="Chia sẻ cảm nhận của bạn về bài viết..."
                rows={3}
                className="w-full p-3 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-100 focus:border-blue-500 resize-none"
              />
            </div>
          </div>

          {/* Footer */}
          <div className="p-5 border-t border-gray-100 flex gap-3 justify-end">
            <button
              onClick={onClose}
              className="px-5 py-2.5 text-gray-600 font-medium hover:bg-gray-100 rounded-xl transition-colors"
            >
              Hủy
            </button>
            <button
              onClick={handleSubmit}
              disabled={selectedRating === 0}
              className="px-5 py-2.5 bg-gradient-to-r from-blue-500 to-indigo-600 text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 flex items-center gap-2"
            >
              <Send size={16} />
              Gửi đánh giá
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

RatingModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  onSubmit: PropTypes.func.isRequired,
  blogTitle: PropTypes.string,
};

export default RatingModal;
