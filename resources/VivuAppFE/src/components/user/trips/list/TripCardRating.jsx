import React from "react";
import { Star } from "lucide-react";
import { motion } from "framer-motion";
import PropTypes from "prop-types";

// Hoisted variants
const fadeIn = {
  hidden: { opacity: 0, y: 4 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.4 } },
};

/**
 * Compact rating display component.
 * Mode 1: Read (Rated) - Shows stars + review snippet
 * Mode 2: CTA (Unrated) - Shows visible Gradient Button to encourage rating
 */
const TripCardRating = React.memo(({ isRated, rating, review, isOwner, onClick }) => {
  if (isRated) {
    // VARIANT: RATED DISPLAY
    return (
      <motion.div
        variants={fadeIn}
        initial="hidden"
        animate="visible"
        className="mt-3 px-1 group/rating cursor-pointer"
        onClick={(e) => {
          e.stopPropagation();
          onClick();
        }}
      >
        <div className="flex items-center gap-1.5 mb-1.5">
          <div className="flex gap-0.5">
            {[1, 2, 3, 4, 5].map((star) => (
              <Star
                key={star}
                className={`w-3.5 h-3.5 ${
                  star <= (rating || 0)
                    ? "fill-yellow-400 text-yellow-400"
                    : "text-gray-200 dark:text-gray-700"
                }`}
              />
            ))}
          </div>
          <span className="text-xs font-bold text-gray-700 dark:text-gray-300">
            {rating}/5
          </span>
        </div>

        {review ? (
          <p className="text-xs text-gray-500 dark:text-gray-400 italic line-clamp-1 group-hover/rating:text-transparent group-hover/rating:bg-clip-text group-hover/rating:bg-gradient-primary transition-all">
            &ldquo;{review}&rdquo;
          </p>
        ) : (
          <p className="text-xs text-gray-400 italic">Đã đánh giá</p>
        )}
      </motion.div>
    );
  }

  // VARIANT: CTA DISPLAY (Unrated) - Now clearly visible
  if (!isOwner) return null;

  return (
    <motion.div
      variants={fadeIn}
      initial="hidden"
      animate="visible"
      className="mt-3 px-1"
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
    >
      <button className="flex items-center gap-2 px-4 py-2 bg-gradient-primary text-white rounded-full text-xs font-bold shadow-md shadow-blue-500/20 hover:shadow-lg hover:shadow-blue-500/30 hover:scale-105 active:scale-95 transition-all duration-300 group/btn">
        <Star className="w-3.5 h-3.5 fill-white/80 group-hover/btn:fill-white transition-colors" />
        Đánh giá chuyến đi
      </button>
    </motion.div>
  );
});

TripCardRating.propTypes = {
  isRated: PropTypes.bool,
  rating: PropTypes.number,
  review: PropTypes.string,
  onClick: PropTypes.func.isRequired,
};

TripCardRating.displayName = "TripCardRating";

export default TripCardRating;
