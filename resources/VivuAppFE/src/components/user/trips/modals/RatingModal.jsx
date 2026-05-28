import React, { useState, useCallback } from "react";
import { createPortal } from "react-dom";
import { Star } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import toast from "../../../../utils/toast";

// --- Compound Modal Components ---

const ModalOverlay = ({ children, onClose }) => (
  <motion.div
    initial={{ opacity: 0 }}
    animate={{ opacity: 1 }}
    exit={{ opacity: 0 }}
    className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4"
    onClick={onClose}
  >
    {children}
  </motion.div>
);

const ModalContent = ({ children }) => (
  <motion.div
    initial={{ scale: 0.95, opacity: 0, y: 20 }}
    animate={{ scale: 1, opacity: 1, y: 0 }}
    exit={{ scale: 0.95, opacity: 0, y: 20 }}
    className="w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-xl dark:bg-gray-900"
    onClick={(e) => e.stopPropagation()}
  >
    {children}
  </motion.div>
);

const ModalHeader = ({ children }) => (
  <div className="border-b border-gray-100 px-6 py-4 dark:border-gray-800">
    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
      {children}
    </h3>
  </div>
);

const ModalBody = ({ children }) => <div className="px-6 py-4">{children}</div>;

const ModalFooter = ({ children }) => (
  <div className="flex justify-end gap-3 bg-gray-50 px-6 py-4 dark:bg-gray-800/50">
    {children}
  </div>
);

const Modal = ({ isOpen, onClose, children }) => {
  if (!isOpen) return null;

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <ModalOverlay onClose={onClose}>
          <ModalContent>{children}</ModalContent>
        </ModalOverlay>
      )}
    </AnimatePresence>,
    document.body,
  );
};

Modal.Header = ModalHeader;
Modal.Body = ModalBody;
Modal.Footer = ModalFooter;

// --- Rating Modal ---

export default function RatingModal({
  isOpen,
  onClose,
  trip,
  onSubmit,
  readOnly = false,
  initialRating = 0,
  initialReview = "",
}) {
  const [rating, setRating] = useState(initialRating);
  const [review, setReview] = useState(initialReview);
  const [hoverRating, setHoverRating] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Update effect to sync with props when opening in read-only mode
  React.useEffect(() => {
    if (isOpen) {
      setRating(initialRating);
      setReview(initialReview);
    }
  }, [isOpen, initialRating, initialReview]);

  const handleSubmit = async () => {
    if (rating === 0) {
      toast.error("Vui lòng chọn số sao đánh giá!");
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit({ rating, review });
      toast.success("Cảm ơn bạn đã đánh giá!");
      onClose();
    } catch (error) {
      toast.error("Lỗi khi gửi đánh giá. Vui lòng thử lại.");
      console.error(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRating = useCallback(
    (value) => {
      if (!readOnly) {
        setRating(value);
      }
    },
    [readOnly],
  );

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <Modal.Header>
        {readOnly ? "⭐ Đánh giá của bạn" : "⭐ Đánh giá chuyến đi"}
      </Modal.Header>
      <Modal.Body>
        <div className="space-y-6">
          <div className="text-center">
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {readOnly
                ? "Bạn đã đánh giá chuyến đi này"
                : "Bạn thấy chuyến đi này thế nào?"}
            </p>
            <h4 className="font-bold text-xl text-gray-900 dark:text-white mt-2">
              {trip?.title}
            </h4>
          </div>

          <div
            className={`flex justify-center gap-3 py-2 ${readOnly ? "pointer-events-none" : ""}`}
          >
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                className={`transition-transform focus:outline-none ${!readOnly ? "hover:scale-110" : ""}`}
                onMouseEnter={() => !readOnly && setHoverRating(star)}
                onMouseLeave={() => !readOnly && setHoverRating(0)}
                onClick={() => handleRating(star)}
                disabled={readOnly}
              >
                <Star
                  className={`h-10 w-10 ${
                    star <= (hoverRating || rating)
                      ? "fill-yellow-400 text-yellow-400 drop-shadow-sm"
                      : "text-gray-200 dark:text-gray-700"
                  }`}
                />
              </button>
            ))}
          </div>

          {readOnly ? (
            <div className="bg-gray-50 dark:bg-gray-800/50 rounded-xl p-6 relative border border-gray-100 dark:border-gray-700">
              <div className="absolute -top-3 left-6 px-2 bg-white dark:bg-gray-900 text-gray-500 text-xs uppercase tracking-wider font-semibold">
                Nhận xét
              </div>
              <p className="text-gray-700 dark:text-gray-300 italic leading-relaxed text-center min-h-[60px] flex items-center justify-center">
                {review ? `"${review}"` : "Không có nhận xét chi tiết."}
              </p>
            </div>
          ) : (
            <div>
              <label className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Nhận xét (tùy chọn)
              </label>
              <textarea
                value={review}
                onChange={(e) => setReview(e.target.value)}
                placeholder="Chia sẻ trải nghiệm của bạn..."
                className="w-full rounded-lg border border-gray-300 p-4 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:border-gray-700 dark:bg-gray-800 dark:text-white resize-none shadow-sm"
                rows={4}
              />
            </div>
          )}
        </div>
      </Modal.Body>
      <Modal.Footer>
        <button
          onClick={onClose}
          className="rounded-lg px-6 py-2.5 text-sm font-medium text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800 transition-colors"
          disabled={isSubmitting}
        >
          {readOnly ? "Đóng" : "Hủy"}
        </button>
        {!readOnly && (
          <button
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="rounded-lg bg-blue-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50 shadow-lg shadow-blue-200 transition-all hover:-translate-y-0.5"
          >
            {isSubmitting ? "Đang gửi..." : "Gửi đánh giá"}
          </button>
        )}
      </Modal.Footer>
    </Modal>
  );
}
