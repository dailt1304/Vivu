import React from "react";
import PropTypes from "prop-types";

/**
 * Reusable section wrapper for settings
 */
const SettingsSection = ({ title, description, children, danger = false }) => {
  return (
    <div
      className={`rounded-[2rem] overflow-hidden transition-all ${
        danger 
          ? "bg-rose-50/30 ring-1 ring-rose-200/50" 
          : "bg-white ring-1 ring-slate-200/60 shadow-[0_8px_30px_rgb(0,0,0,0.04)]"
      }`}
    >
      {/* Header */}
      <div
        className={`px-8 py-5 border-b ${
          danger ? "border-rose-100 bg-rose-50/50" : "border-slate-100/80"
        }`}
      >
        <h3
          className={`text-lg font-bold tracking-tight ${danger ? "text-rose-700" : "text-slate-900"}`}
        >
          {title}
        </h3>
        {description && (
          <p
            className={`text-sm mt-1 font-medium ${danger ? "text-rose-600/80" : "text-slate-500"}`}
          >
            {description}
          </p>
        )}
      </div>

      {/* Content */}
      <div className="p-8">{children}</div>
    </div>
  );
};

SettingsSection.propTypes = {
  title: PropTypes.string.isRequired,
  description: PropTypes.string,
  children: PropTypes.node.isRequired,
  danger: PropTypes.bool,
};

export default SettingsSection;
