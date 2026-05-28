import React from "react";
import { Star } from "lucide-react";
import PropTypes from "prop-types";

/**
 * RatingStars component - displays star ratings
 * @param {number} rating - The rating value (0-5)
 * @param {number} count - Number of ratings
 * @param {string} size - Size variant: 'sm', 'md', 'lg'
 * @param {boolean} showCount - Whether to show the count
 * @param {boolean} interactive - Whether stars are clickable
 * @param {function} onRate - Callback when a star is clicked
 */
const RatingStars = ({
  rating = 0,
  count = 0,
  size = "sm",
  showCount = true,
  interactive = false,
  onRate = () => {},
}) => {
  const sizeClasses = {
    sm: "w-3 h-3",
    md: "w-4 h-4",
    lg: "w-5 h-5",
  };

  const textSizes = {
    sm: "text-xs",
    md: "text-sm",
    lg: "text-base",
  };

  const stars = [1, 2, 3, 4, 5];

  return (
    <div className="flex items-center gap-1">
      <div className="flex items-center">
        {stars.map((star) => {
          const filled = star <= Math.round(rating);
          const halfFilled = star === Math.ceil(rating) && rating % 1 !== 0;

          return (
            <button
              key={star}
              type="button"
              onClick={() => interactive && onRate(star)}
              disabled={!interactive}
              className={`${interactive ? "cursor-pointer hover:scale-110 transition-transform" : "cursor-default"} focus:outline-none`}
            >
              <Star
                className={`${sizeClasses[size]} ${
                  filled
                    ? "text-amber-400 fill-amber-400"
                    : halfFilled
                      ? "text-amber-400 fill-amber-200"
                      : "text-gray-300"
                } transition-colors`}
              />
            </button>
          );
        })}
      </div>

      {showCount && count > 0 && (
        <span className={`${textSizes[size]} text-gray-500 ml-1`}>
          <span className="font-semibold text-gray-700">
            {rating.toFixed(1)}
          </span>
          <span className="text-gray-400 ml-0.5">({count})</span>
        </span>
      )}
    </div>
  );
};

RatingStars.propTypes = {
  rating: PropTypes.number,
  count: PropTypes.number,
  size: PropTypes.oneOf(["sm", "md", "lg"]),
  showCount: PropTypes.bool,
  interactive: PropTypes.bool,
  onRate: PropTypes.func,
};

export default RatingStars;
