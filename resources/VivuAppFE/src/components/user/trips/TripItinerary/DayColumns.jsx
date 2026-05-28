import React from "react";
import { useTripItineraryContext } from "./TripItineraryContext";
import DayColumn from "../itinerary/DayColumn";

/**
 * DayColumns
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Renders the day columns with scheduled items.
 * Matches the original TripDetailsDrawer implementation.
 */
function DayColumns({
  getDateLabel,
  onEditTime,
  onDelete,
  onViewDetails,
  onTimeChange,
  activeMenuId,
  onToggleMenu,
  onMoveToIdeas,
  onNoteChange,
  className = "",
}) {
  const { state, meta } = useTripItineraryContext();
  const { items } = state;
  const { dayIds, onItemHover, onItemLeave } = meta;

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Days Header */}
      <div className="flex items-center justify-between">
        <h3 className="font-bold text-slate-800 text-lg">
          📅 Lịch trình ({dayIds.length} ngày)
        </h3>
      </div>

      {/* Day Columns */}
      <div className="space-y-4">
        {dayIds.map((dayId, index) => (
          <DayColumn
            key={dayId}
            dayId={dayId}
            dayNumber={index + 1}
            dateLabel={getDateLabel?.(index) || `Ngày ${index + 1}`}
            items={items[dayId] || []}
            activeMenuId={activeMenuId}
            onToggleMenu={onToggleMenu}
            onTimeChange={onTimeChange}
            onViewDetails={onViewDetails}
            onEditTime={onEditTime}
            onDelete={(id) => onDelete?.(id, dayId)}
            onMoveToIdeas={onMoveToIdeas}
            onItemHover={onItemHover}
            onItemLeave={onItemLeave}
            onNoteChange={onNoteChange}
          />
        ))}
      </div>
    </div>
  );
}

export default React.memo(DayColumns);
