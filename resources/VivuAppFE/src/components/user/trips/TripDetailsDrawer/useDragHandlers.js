import { useState, useCallback, useRef, useEffect } from "react";
import { useIdeasToSchedule } from "../TripItinerary/hooks/useIdeasToSchedule";
import { useScheduleToIdeas } from "../TripItinerary/hooks/useScheduleToIdeas";
import { useDayToDay } from "../TripItinerary/hooks/useDayToDay";
import { useReorderItems } from "../TripItinerary/hooks/useReorderItems";
import { useDragOverHandler } from "../TripItinerary/hooks/useDragOverHandler";

/**
 * useDragHandlers - Custom hook for drag-and-drop functionality
 *
 * Refactored to Vercel Standards:
 * Acts as an orchestrator for specialized hooks.
 */
export function useDragHandlers({
  items,
  setItems,
  tripDayIdMap,
  dayIds,
  onReorderSuccess,
}) {
  // DnD State
  const [activeId, setActiveId] = useState(null);
  const [dragStartContainer, setDragStartContainer] = useState(null);

  // Ref for items to avoid stale closure issues
  const itemsRef = useRef(items);
  useEffect(() => {
    itemsRef.current = items;
  }, [items]);

  /**
   * Find which container an item belongs to
   */
  const findContainer = useCallback((id) => {
    if (!id) return null;
    if (id in itemsRef.current) return id;
    return Object.keys(itemsRef.current).find(
      (key) => itemsRef.current[key]?.find((item) => item.id === id)?.id,
    );
  }, []);

  // --- Specialized Hooks ---

  const {
    currentOverContainer,
    setCurrentOverContainer,
    dragOverInfo,
    setDragOverInfo, // Exposed for reset
    handleDragOver,
  } = useDragOverHandler({
    items,
    dayIds,
    findContainer,
  });

  const { handleIdeasToScheduleDrop } = useIdeasToSchedule({
    items,
    setItems,
    tripDayIdMap,
  });

  const { handleScheduleToIdeasDrop } = useScheduleToIdeas({
    items,
    setItems,
  });

  const { handleDayToDayDrop } = useDayToDay({
    items,
    setItems,
    tripDayIdMap,
  });

  const { handleReorder } = useReorderItems({
    items,
    setItems,
    tripDayIdMap,
    onReorderSuccess,
  });

  /**
   * Handle drag start event
   */
  const handleDragStart = useCallback(
    (event) => {
      const container = findContainer(event.active.id);
      setDragStartContainer(container);
      setActiveId(event.active.id);
    },
    [findContainer],
  );

  /**
   * Handle drag end event - finalizes drop and calls API
   */
  const handleDragEnd = useCallback(
    (event) => {
      const { active, over } = event;

      // Reset indicator
      setDragOverInfo((prev) => {
        if (prev.dayId === null) return prev;
        return { dayId: null, index: null, time: null };
      });
      setCurrentOverContainer(null);

      const activeContainer = dragStartContainer;
      const overContainer = findContainer(over?.id);

      if (activeContainer && overContainer) {
        // Case 1: Ideas -> Schedule
        if (activeContainer === "ideas" && overContainer !== "ideas") {
          handleIdeasToScheduleDrop(active, over, dragOverInfo);
        }
        // Case 2: Schedule -> Ideas
        else if (activeContainer !== "ideas" && overContainer === "ideas") {
          handleScheduleToIdeasDrop(active, activeContainer);
        }
        // Case 3: Reorder
        else if (activeContainer === overContainer) {
          handleReorder(active, over, activeContainer, overContainer);
        }
        // Case 4: Day -> Day
        else {
          handleDayToDayDrop(
            active,
            over,
            activeContainer,
            overContainer,
            dragOverInfo,
          );
        }
      }

      setActiveId(null);
      setDragStartContainer(null);
    },
    [
      dragStartContainer,
      findContainer,
      dragOverInfo,
      handleIdeasToScheduleDrop,
      handleScheduleToIdeasDrop,
      handleReorder,
      handleDayToDayDrop,
      setCurrentOverContainer,
      setDragOverInfo,
    ],
  );

  // Drop animation config
  const dropAnimation = {
    sideEffects: () => {},
  };

  return {
    activeId,
    currentOverContainer,
    dragOverInfo,
    handleDragStart,
    handleDragOver,
    handleDragEnd,
    dropAnimation,
    findContainer,
  };
}

export default useDragHandlers;
