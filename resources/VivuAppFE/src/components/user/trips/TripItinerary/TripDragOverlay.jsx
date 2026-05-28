import React from "react";
import { DragOverlay } from "@dnd-kit/core";
import { restrictToWindowEdges } from "@dnd-kit/modifiers";
import { useTripItineraryContext } from "./TripItineraryContext";

/**
 * TripDragOverlay
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Renders the drag overlay that shows the item being dragged.
 * Uses morphing animation when crossing container boundaries.
 */
function TripDragOverlay() {
  const { state, meta } = useTripItineraryContext();
  const { activeId, items, currentOverContainer } = state;
  const { dropAnimation } = meta;

  // Find the active item
  const activeItem = React.useMemo(() => {
    if (!activeId) return null;

    for (const containerId of Object.keys(items)) {
      const item = items[containerId]?.find((i) => i.id === activeId);
      if (item) return item;
    }
    return null;
  }, [activeId, items]);

  if (!activeItem) return null;

  // Determine if morphing to different format
  const isOverIdeas = currentOverContainer === "ideas";
  const isIdea = activeItem.type === "idea";

  return (
    <DragOverlay dropAnimation={dropAnimation} modifiers={[restrictToWindowEdges]}>
      <div
        className={`
          pointer-events-none
          ${isOverIdeas ? "opacity-90" : "opacity-100"}
        `}
      >
        {/* Simplified drag preview */}
        <div
          className={`
            p-3 rounded-lg shadow-xl border-2 border-blue-400 bg-white
            ${isIdea ? "w-40" : "w-64"}
          `}
        >
          {/* Image preview if available */}
          {activeItem.image && (
            <div className="w-full h-16 rounded-md overflow-hidden mb-2">
              <img
                src={activeItem.image}
                alt={activeItem.content}
                className="w-full h-full object-cover"
              />
            </div>
          )}

          {/* Content */}
          <p className="font-medium text-sm text-slate-800 truncate">
            {activeItem.content}
          </p>

          {/* Time badge for scheduled items */}
          {activeItem.startTime && !isOverIdeas && (
            <span className="text-xs text-blue-600 bg-blue-50 px-2 py-0.5 rounded mt-1 inline-block">
              {activeItem.startTime}
              {activeItem.endTime && ` - ${activeItem.endTime}`}
            </span>
          )}
        </div>
      </div>
    </DragOverlay>
  );
}

export default React.memo(TripDragOverlay);
