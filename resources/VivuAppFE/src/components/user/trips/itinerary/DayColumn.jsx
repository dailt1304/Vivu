import React, { useMemo } from "react";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import DroppableContainer from "./DroppableContainer";
import SortableItem from "./SortableItem";
import TripDayRouteSummary from "../TripDayRouteSummary";
import { useRouteDirection } from "../../../../hooks/maps/useRouteDirection";

const DayColumn = ({
  dayId,
  dayNumber,
  dateLabel,
  items,
  activeMenuId,
  onToggleMenu,
  onTimeChange,
  onEditTime,
  onViewDetails,
  onDelete,
  onMoveToIdeas,
  onItemHover,
  onItemLeave,
  onNoteChange,
}) => {
  const dayLocations = useMemo(() => {
    return items.map((loc) => ({
      id: loc.locationId || loc.id,
      latitude: loc.latitude,
      longitude: loc.longitude,
    }));
  }, [items]);

  const { routeData, isLoading: isRouteLoading } = useRouteDirection(dayLocations);

  return (
    <div className="mb-8 relative">
      <h4 className="flex items-center justify-between pb-3 border-b border-gray-100/80 mb-4">
        <div className="flex items-center gap-3">
          <span className="w-8 h-8 rounded-xl bg-gradient-primary text-white flex items-center justify-center text-sm font-black shadow-md shadow-blue-500/20">
            {dayNumber}
          </span>
          <div className="flex flex-col">
            <span className="text-[15px] font-black tracking-tight uppercase text-gray-900 leading-tight">
              Ngày {dayNumber}
            </span>
            <span className="text-[11px] text-gray-400 font-bold tracking-wider">
              {dateLabel}
            </span>
          </div>
        </div>
      </h4>

      {/* Vercel Rule: rendering-conditional-render */}
      <div className="mb-3 pl-8">
        <TripDayRouteSummary
          routeData={routeData}
          isLoading={isRouteLoading}
          stopCount={items.length}
        />
      </div>

      <SortableContext
        id={dayId}
        items={items.map((i) => i.id)}
        strategy={verticalListSortingStrategy}
      >
        <DroppableContainer
          id={dayId}
          className="space-y-2 min-h-[100px] p-2 rounded-xl transition-all relative"
        >
          {items.map((item) => (
            <React.Fragment key={item.id}>
              <SortableItem
                key={item.id}
                id={item.id}
                item={item}
                type="item"
                onTimeChange={onTimeChange}
                onEditTime={onEditTime}
                activeMenuId={activeMenuId}
                onToggleMenu={onToggleMenu}
                onViewDetails={onViewDetails}
                onDelete={onDelete}
                onMoveToIdeas={onMoveToIdeas}
                onItemHover={onItemHover}
                onItemLeave={onItemLeave}
                onNoteChange={onNoteChange}
              />
            </React.Fragment>
          ))}
        </DroppableContainer>
      </SortableContext>
    </div>
  );
};

export default React.memo(DayColumn, (prevProps, nextProps) => {
  return (
    prevProps.dayId === nextProps.dayId &&
    prevProps.items === nextProps.items &&
    prevProps.activeMenuId === nextProps.activeMenuId
  );
});
