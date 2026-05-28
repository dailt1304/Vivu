import React from "react";
import { motion } from "framer-motion";
import PropTypes from "prop-types";

/**
 * Stats card with animated counter
 */
const ProfileStatsCard = ({
  icon: Icon,
  value,
  label,
  gradient = "from-blue-500 to-indigo-600",
  onClick,
}) => {
  return (
    <motion.div
      whileHover={{ scale: 1.02, y: -2 }}
      whileTap={{ scale: 0.98 }}
      onClick={onClick}
      className={`relative overflow-hidden bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-lg transition-all cursor-pointer group`}
    >
      {/* Gradient accent */}
      <div
        className={`absolute top-0 left-0 right-0 h-1 bg-gradient-to-r ${gradient}`}
      />

      <div className="p-5">
        <div className="flex items-center justify-between mb-3">
          <div
            className={`p-2.5 rounded-xl bg-gradient-to-br ${gradient} text-white shadow-lg`}
          >
            <Icon size={20} />
          </div>
          <motion.div
            initial={{ opacity: 0, scale: 0.5 }}
            animate={{ opacity: 1, scale: 1 }}
            className="text-2xl font-black text-gray-900"
          >
            {typeof value === "number" ? value.toLocaleString() : value}
          </motion.div>
        </div>
        <p className="text-sm font-medium text-gray-500 group-hover:text-gray-700 transition-colors">
          {label}
        </p>
      </div>
    </motion.div>
  );
};

ProfileStatsCard.propTypes = {
  icon: PropTypes.elementType.isRequired,
  value: PropTypes.oneOfType([PropTypes.number, PropTypes.string]).isRequired,
  label: PropTypes.string.isRequired,
  gradient: PropTypes.string,
  onClick: PropTypes.func,
};

export default ProfileStatsCard;
