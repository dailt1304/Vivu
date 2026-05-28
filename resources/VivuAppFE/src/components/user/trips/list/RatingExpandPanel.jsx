import React, { useState, useMemo } from "react";
import { Star, MapPin, Calendar, Clock, X } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import PropTypes from "prop-types";

// Static animation variants (hoisted)
const panelVariants = {
  hidden: { opacity: 0, height: 0, marginTop: 0 },
  visible: {
    opacity: 1,
    height: "auto",
    marginTop: 24,
    transition: { duration: 0.4, ease: [0.32, 0.72, 0, 1] },
  },
  exit: {
    opacity: 0,
    height: 0,
    marginTop: 0,
    transition: { duration: 0.3, ease: [0.32, 0.72, 0, 1] },
  },
};

const contentVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0, transition: { delay: 0.1, duration: 0.3 } },
};

/**
 * Full-width expansion panel for writing/editing reviews.
 * Appears below the trip grid row.
 */
const RatingExpandPanel = React.memo(({ trip, onSubmit, onClose }) => {
  // Lazy state init for performance
  const [rating, setRating] = useState(() => trip.rating || 0);
  const [review, setReview] = useState(() => trip.review || "");
  const [hoverRating, setHoverRating] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Derived state (no useEffect)
  const isValid = rating > 0;

  const handleSubmit = async () => {
    if (!isValid) return;
    setIsSubmitting(true);
    try {
      await onSubmit(trip.id, { rating, review });
      onClose(); // Close on success
    } catch {
      setIsSubmitting(false);
    }
  };

  // Memoize star buttons to avoid re-renders during text input
  const starButtons = useMemo(
    () => (
      <div className="flex gap-2 mb-4">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type="button"
            className="transition-transform hover:scale-110 focus:outline-none"
            onMouseEnter={() => setHoverRating(star)}
            onMouseLeave={() => setHoverRating(0)}
            onClick={() => setRating(star)}
            disabled={isSubmitting}
          >
            <Star
              className={`w-8 h-8 md:w-10 md:h-10 transition-colors ${
                star <= (hoverRating || rating)
                  ? "fill-yellow-400 text-yellow-400 drop-shadow-sm"
                  : "text-gray-300 dark:text-gray-600"
              }`}
            />
          </button>
        ))}
      </div>
    ),
    [hoverRating, rating, isSubmitting],
  );

  return (
    <motion.div
      variants={panelVariants}
      initial="hidden"
      animate="visible"
      exit="exit"
      className="w-full relative col-span-full overflow-hidden"
    >
      {/* Connector triangle */}
      <div className="absolute top-0 left-1/2 -translate-x-1/2 -mt-2 w-0 h-0 border-l-[12px] border-l-transparent border-r-[12px] border-r-transparent border-b-[12px] border-b-white dark:border-b-gray-800 drop-shadow-sm z-10" />

      <div className="bg-white dark:bg-gray-800 rounded-3xl shadow-xl shadow-blue-100/50 dark:shadow-none border border-white dark:border-gray-700 overflow-hidden relative ring-1 ring-blue-500/10">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 z-10"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex flex-col md:flex-row h-full">
          {/* Left: Trip Info & Image */}
          <div className="w-full md:w-5/12 lg:w-4/12 relative min-h-[200px] md:min-h-[350px]">
            <img
              src={trip.image}
              alt={trip.title}
              className="w-full h-full object-cover"
            />
            <div className="absolute inset-0 bg-linear-to-t from-black/80 via-black/20 to-transparent" />
            <div className="absolute bottom-6 left-6 right-6 text-white">
              <h3 className="text-2xl font-bold mb-2 leading-tight">
                {trip.title}
              </h3>
              <div className="flex flex-wrap gap-3 text-sm font-medium text-white/90">
                <div className="flex items-center gap-1.5 bg-white/20 backdrop-blur-md px-3 py-1 rounded-full">
                  <Calendar className="w-4 h-4" />
                  {trip.dates}
                </div>
                <div className="flex items-center gap-1.5 bg-white/20 backdrop-blur-md px-3 py-1 rounded-full">
                  <Clock className="w-4 h-4" />
                  {trip.days} ngày
                </div>
              </div>
            </div>
          </div>

          {/* Right: Rating Form */}
          <motion.div
            variants={contentVariants}
            className="flex-1 p-6 md:p-8 lg:p-10 flex flex-col justify-center bg-gray-50/50 dark:bg-gray-800/50"
          >
            {trip.isRated ? (
              <div className="flex flex-col h-full justify-center">
                <h4 className="text-xl font-bold text-gray-800 dark:text-white mb-6">
                  {trip.isOwner ? "Đánh giá của bạn" : "Đánh giá của chủ chuyến đi"}
                </h4>

                <div className="flex gap-2 mb-4">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <Star
                      key={star}
                      className={`w-8 h-8 md:w-10 md:h-10 transition-colors ${
                        star <= (trip.rating || 0)
                          ? "fill-yellow-400 text-yellow-400 drop-shadow-sm"
                          : "text-gray-300 dark:text-gray-600"
                      }`}
                    />
                  ))}
                </div>

                {trip.review ? (
                  <p className="text-gray-700 dark:text-gray-200 text-lg leading-relaxed mt-4 italic bg-white dark:bg-gray-900 p-6 rounded-2xl border border-gray-100 dark:border-gray-700 shadow-sm">
                    &ldquo;{trip.review}&rdquo;
                  </p>
                ) : (
                  <p className="text-gray-400 mt-4 italic">
                    Không có nhận xét chi tiết.
                  </p>
                )}

                <div className="mt-8 flex justify-end">
                  <button
                    onClick={onClose}
                    className="px-8 py-2.5 rounded-xl font-bold text-white bg-gradient-primary hover:shadow-lg hover:-translate-y-0.5 transition-all"
                  >
                    Đóng
                  </button>
                </div>
              </div>
            ) : (
              <>
                <h4 className="text-xl font-bold text-gray-800 dark:text-white mb-2">
                  Đánh giá chuyến đi
                </h4>
                <p className="text-gray-500 dark:text-gray-400 text-sm mb-6">
                  Trải nghiệm của bạn như thế nào? Hãy chia sẻ để cộng đồng cùng
                  biết nhé!
                </p>

                {starButtons}

                <div className="mb-6 relative">
                  <textarea
                    value={review}
                    onChange={(e) => setReview(e.target.value)}
                    maxLength={150}
                    placeholder="Chia sẻ cảm nhận chi tiết của bạn về chuyến đi..."
                    className="w-full p-4 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all resize-none min-h-[120px] text-gray-700 dark:text-gray-200 pb-8"
                    disabled={isSubmitting}
                  />
                  <div
                    className={`absolute bottom-3 right-4 text-xs font-medium ${review.length >= 150 ? "text-orange-500" : "text-gray-400"}`}
                  >
                    {review.length}/150 ký tự (khoảng 20 từ)
                  </div>
                </div>

                <div className="flex items-center gap-4">
                  <button
                    onClick={onClose}
                    className="px-6 py-2.5 rounded-xl font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                    disabled={isSubmitting}
                  >
                    Hủy bỏ
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={!isValid || isSubmitting}
                    className="px-8 py-2.5 rounded-xl font-bold text-white bg-gradient-primary hover:shadow-lg hover:shadow-blue-500/30 hover:-translate-y-0.5 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:shadow-none disabled:hover:translate-y-0 transition-all ml-auto"
                  >
                    {isSubmitting ? "Đang gửi..." : "Gửi đánh giá"}
                  </button>
                </div>
              </>
            )}
          </motion.div>
        </div>
      </div>
    </motion.div>
  );
});

RatingExpandPanel.propTypes = {
  trip: PropTypes.object.isRequired,
  onSubmit: PropTypes.func.isRequired,
  onClose: PropTypes.func.isRequired,
};

RatingExpandPanel.displayName = "RatingExpandPanel";

export default RatingExpandPanel;
