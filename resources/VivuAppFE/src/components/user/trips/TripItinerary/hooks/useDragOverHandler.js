import { useState, useCallback } from "react";

/**
 * Hook to handle drag over events and state
 */
export function useDragOverHandler({ items, findContainer }) {
  const [currentOverContainer, setCurrentOverContainer] = useState(null);
  const [dragOverInfo, setDragOverInfo] = useState({
    dayId: null,
    index: null,
    time: null,
  });

  const handleDragOver = useCallback(
    (event) => {
      const { active, over } = event;
      const overId = over?.id;
      const effectiveOverId = overId || (active.id === overId ? null : overId);
      const overContainer = findContainer(effectiveOverId);

      // 1. Update Current Over Container State
      if (overContainer) {
        setCurrentOverContainer(overContainer);
      } else if (overId && String(overId).startsWith("day-")) {
        setCurrentOverContainer(overId);
      } else if (overId === "ideas") {
        setCurrentOverContainer("ideas");
      } else {
        if (!over) setCurrentOverContainer(null);
      }

      // 2. Handle Drag Over Day Columns (Time Calculation - REMOVED)
      const targetContainer =
        overContainer ||
        (overId && String(overId).startsWith("day-") ? overId : null);

      if (targetContainer && targetContainer.startsWith("day-")) {
        // Time indicator removed as per user request
        const calculatedTime = null;
        let newIndex = 0;

        // Calculate Index
        const overItems = items[targetContainer] || [];
        const overIndex = overItems.findIndex((i) => i.id === overId);

        if (over && over.data.current?.sortable) {
          const isBelowOverItem =
            active.rect.current.translated &&
            active.rect.current.translated.top >
              over.rect.top + over.rect.height / 2;
          newIndex =
            overIndex >= 0
              ? overIndex + (isBelowOverItem ? 1 : 0)
              : overItems.length;
        } else {
          newIndex = overItems.length;
        }

        // Debounce/Throttling optimization: Only update if changed
        setDragOverInfo((prev) => {
          if (
            prev.dayId === targetContainer &&
            prev.index === newIndex &&
            prev.time === calculatedTime
          )
            return prev;

          return {
            dayId: targetContainer,
            index: newIndex,
            time: calculatedTime,
          };
        });
      } else {
        // Reset info if not over a day
        setDragOverInfo((prev) => {
          if (prev.dayId === null) return prev;
          return { dayId: null, index: null, time: null };
        });
      }
    },
    [items, findContainer],
  );

  return {
    currentOverContainer,
    setCurrentOverContainer,
    dragOverInfo,
    setDragOverInfo,
    handleDragOver,
  };
}
