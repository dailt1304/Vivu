import React from "react";
import { SortableContext, rectSortingStrategy } from "@dnd-kit/sortable";
import { useDroppable } from "@dnd-kit/core";
import { Plus, Check } from "lucide-react";
import { useTripItineraryContext } from "./TripItineraryContext";
import SortableItem from "../itinerary/SortableItem";

/**
 * IdeasPanel
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Renders the ideas panel with draggable idea items in a grid layout.
 * Matches the original TripDetailsDrawer implementation.
 */
function IdeasPanel({
  onAddLocation,
  onViewDetails,
  onAddToSchedule,
  onDelete,
  activeMenuId,
  onToggleMenu,
  onSwapLocation,
  onQuickAddToDay,
  dayOptions,
  className = "",
}) {
  const { state, meta } = useTripItineraryContext();
  const { items, currentOverContainer, activeId } = state;
  const { onItemHover, onItemLeave, findContainer } = meta;

  // Create droppable for ideas container
  const { setNodeRef } = useDroppable({ id: "ideas" });

  const ideas = items.ideas || [];
  const isDropTarget = currentOverContainer === "ideas";

  // Check if dragging from schedule to ideas
  const isDraggingFromSchedule =
    activeId && findContainer?.(activeId) !== "ideas";
  const showDropZone = isDraggingFromSchedule && isDropTarget;

  return (
    <div className={className}>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-extrabold text-gray-900 text-lg tracking-tight">
          💡 Ý tưởng
          <span className="ml-2 text-[13px] px-2 py-0.5 bg-gray-100 text-gray-500 rounded-full">
            {ideas.length}
          </span>
        </h3>
      </div>

      {/* Ideas Grid */}
      <SortableContext
        id="ideas"
        items={ideas.map((i) => i.id)}
        strategy={rectSortingStrategy}
      >
        <div
          ref={setNodeRef}
          className={`
            grid grid-cols-2 md:grid-cols-4 gap-3 px-1 min-h-[140px] 
            transition-colors rounded-xl
            ${isDropTarget ? "bg-blue-50/30" : ""}
          `}
        >
          {ideas.map((item) => (
            <SortableItem
              key={item.id}
              id={item.id}
              item={item}
              type="idea"
              activeMenuId={activeMenuId}
              onToggleMenu={onToggleMenu}
              onViewDetails={() => onViewDetails?.(item)}
              onAddToSchedule={() => onAddToSchedule?.(item.id)}
              onQuickAddToDay={onQuickAddToDay}
              dayOptions={dayOptions}
              onSwapLocation={onSwapLocation ? () => onSwapLocation(item.id) : undefined}
              onDelete={() => onDelete?.(item.id)}
              onMouseEnter={() => onItemHover?.(item.id)}
              onMouseLeave={() => onItemLeave?.()}
            />
          ))}

          {/* Add Button - Transforms to Drop Zone when dragging from schedule */}
          <button
            onClick={onAddLocation}
            className={`
              aspect-square rounded-3xl border-2 border-dashed 
              flex flex-col items-center justify-center p-4 text-center
              transition-all duration-300 font-bold text-[13px] gap-3 group
              ${
                showDropZone
                  ? "border-blue-400 bg-blue-50/50 text-blue-700 scale-[1.02] shadow-[inset_0_4px_20px_rgba(59,130,246,0.1)]"
                  : "border-gray-200 text-gray-400 hover:text-gray-700 hover:border-gray-300 hover:bg-gray-50/50"
              }
            `}
          >
            <div
              className={`
                p-3 rounded-2xl transition-all duration-300
                ${
                  showDropZone
                    ? "bg-blue-100 text-blue-600 scale-110"
                    : "bg-gray-100/80 text-gray-400 group-hover:bg-gray-200 group-hover:text-gray-700 group-hover:scale-105"
                }
              `}
            >
              {showDropZone ? <Check size={28} /> : <Plus size={24} />}
            </div>
            <span className="tracking-tight">
              {showDropZone ? "Thả vào đây" : "Thêm ý tưởng"}
            </span>
          </button>
        </div>
      </SortableContext>
    </div>
  );
}

export default React.memo(IdeasPanel);
