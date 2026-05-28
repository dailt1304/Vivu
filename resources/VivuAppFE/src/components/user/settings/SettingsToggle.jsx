import React from "react";
import { motion } from "framer-motion";
import PropTypes from "prop-types";

/**
 * Toggle switch component for settings
 */
const SettingsToggle = ({
  label,
  description,
  checked,
  onChange,
  disabled = false,
}) => {
  return (
    <div className="flex items-start justify-between gap-4 py-3">
      <div className="flex-1">
        <p
          className={`font-medium ${disabled ? "text-gray-400" : "text-gray-900"}`}
        >
          {label}
        </p>
        {description && (
          <p
            className={`text-sm mt-0.5 ${disabled ? "text-gray-300" : "text-gray-500"}`}
          >
            {description}
          </p>
        )}
      </div>

      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={() => !disabled && onChange?.(!checked)}
        disabled={disabled}
        className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 ${
          disabled
            ? "cursor-not-allowed opacity-50"
            : checked
              ? "bg-blue-600"
              : "bg-gray-200"
        }`}
      >
        <motion.span
          layout
          className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-lg ring-0 ${
            checked ? "translate-x-5" : "translate-x-0"
          }`}
          transition={{ type: "spring", stiffness: 500, damping: 30 }}
        />
      </button>
    </div>
  );
};

SettingsToggle.propTypes = {
  label: PropTypes.string.isRequired,
  description: PropTypes.string,
  checked: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired,
  disabled: PropTypes.bool,
};

export default SettingsToggle;
