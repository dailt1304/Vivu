import React from "react";
import { motion } from "framer-motion";
import { Lock } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Achievement badge with locked/unlocked states
 */
const AchievementBadge = ({
  icon,
  title,
  description,
  unlocked = false,
  progress = null, // { current, target, percentage }
  size = "md",
  onClick,
}) => {
  const sizeClasses = {
    sm: "w-14 h-14",
    md: "w-18 h-18",
    lg: "w-24 h-24",
  };

  const iconSizes = {
    sm: "text-2xl",
    md: "text-3xl",
    lg: "text-4xl",
  };

  return (
    <motion.div
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      onClick={onClick}
      className="flex flex-col items-center cursor-pointer group"
    >
      {/* Badge Circle */}
      <div
        className={`relative ${sizeClasses[size]} rounded-full flex items-center justify-center transition-all ${
          unlocked
            ? "bg-gradient-to-br from-amber-400 via-yellow-500 to-orange-500 shadow-lg shadow-amber-200 ring-4 ring-amber-100"
            : "bg-gray-100 ring-2 ring-gray-200"
        }`}
      >
        {/* Icon */}
        <span
          className={`${iconSizes[size]} ${unlocked ? "" : "grayscale opacity-50"}`}
        >
          {icon}
        </span>

        {/* Lock overlay for locked achievements */}
        {!unlocked && (
          <div className="absolute inset-0 flex items-center justify-center bg-gray-900/20 rounded-full">
            <Lock size={16} className="text-gray-500" />
          </div>
        )}

        {/* Glow effect for unlocked */}
        {unlocked && (
          <div className="absolute inset-0 rounded-full bg-gradient-to-br from-amber-400 to-orange-500 blur-md opacity-30 -z-10" />
        )}
      </div>

      {/* Title */}
      <p
        className={`mt-2 text-xs font-semibold text-center max-w-[80px] line-clamp-2 ${
          unlocked ? "text-gray-900" : "text-gray-400"
        }`}
      >
        {title}
      </p>

      {/* Progress bar (if not unlocked and has progress) */}
      {!unlocked && progress && (
        <div className="w-16 mt-1">
          <div className="h-1 bg-gray-200 rounded-full overflow-hidden">
            <motion.div
              initial={{ width: 0 }}
              animate={{ width: `${progress.percentage}%` }}
              className="h-full bg-gradient-to-r from-blue-400 to-blue-600 rounded-full"
            />
          </div>
          <p className="text-[10px] text-gray-400 text-center mt-0.5">
            {progress.current}/{progress.target}
          </p>
        </div>
      )}

      {/* Tooltip on hover */}
      <div className="absolute -top-12 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-20">
        <div className="bg-gray-900 text-white text-xs px-3 py-2 rounded-lg shadow-lg whitespace-nowrap">
          {description}
          <div className="absolute bottom-0 left-1/2 -translate-x-1/2 translate-y-1/2 rotate-45 w-2 h-2 bg-gray-900" />
        </div>
      </div>
    </motion.div>
  );
};

AchievementBadge.propTypes = {
  icon: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  description: PropTypes.string.isRequired,
  unlocked: PropTypes.bool,
  progress: PropTypes.shape({
    current: PropTypes.number,
    target: PropTypes.number,
    percentage: PropTypes.number,
  }),
  size: PropTypes.oneOf(["sm", "md", "lg"]),
  onClick: PropTypes.func,
};

export default AchievementBadge;
