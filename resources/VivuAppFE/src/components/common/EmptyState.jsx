import React from "react";
import { cn } from "@/lib/utils";

/**
 * Reusable component for empty states across the application.
 * Follows the visual style found in user profile tabs.
 * 
 * @param {object} props
 * @param {React.ElementType} props.icon - Lucide icon component
 * @param {string} props.title - Principal message
 * @param {string} props.subtitle - Secondary message/instructions
 * @param {string} props.actionLabel - Text for the CTA button
 * @param {Function} props.onAction - Click handler for the CTA button
 * @param {string} props.className - Additional container classes
 */
const EmptyState = ({
  icon: Icon,
  title,
  subtitle,
  actionLabel,
  onAction,
  className,
}) => {
  return (
    <div className={cn("flex flex-col items-center justify-center p-12 text-gray-400", className)}>
      {Icon && <Icon size={48} className="mb-4 opacity-20" />}
      
      <h3 className="text-lg font-medium text-gray-600 mb-2 text-center">
        {title}
      </h3>
      
      {subtitle && (
        <p className="text-sm text-center max-w-xs">
          {subtitle}
        </p>
      )}
      
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="mt-6 px-6 py-2 bg-blue-50 text-blue-600 font-semibold rounded-xl hover:bg-blue-100 transition-colors active:scale-95 duration-200"
        >
          {actionLabel}
        </button>
      )}
    </div>
  );
};

export default EmptyState;
