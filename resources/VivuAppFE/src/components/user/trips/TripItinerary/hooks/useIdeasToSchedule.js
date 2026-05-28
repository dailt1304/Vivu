import { useCallback } from "react";
import tripLocationApi from "../../../../../api/tripLocationApi";
import toast from "../../../../../utils/toast";
import {
  calculateEndTime,
  getSmartDropTime,
} from "../utils/dragTimeCalculator";

/**
 * Hook to handle moving items from Ideas to Schedule
 */
export function useIdeasToSchedule({ items, setItems, tripDayIdMap }) {
  const handleIdeasToScheduleDrop = useCallback(
    async (active, over, dragOverInfo) => {
      const activeId = active.id;
      const overId = over?.id;

      // Find the active item in ideas
      const activeItem = items.ideas?.find((i) => i.id === activeId);
      if (!activeItem) return;

      const overContainer = over.data.current?.sortable?.containerId || overId;
      // If dropping directly on a day container vs on an item in the day
      const targetDayId = String(overContainer).startsWith("day-")
        ? overContainer
        : null;

      if (!targetDayId) return; // Should not happen if guard checks are correct

      const overItems = items[targetDayId] || [];
      const overIndex = overItems.findIndex((i) => i.id === overId);

      // Calculate new index
      let newIndex;
      if (overIndex >= 0) {
        const isBelowOverItem =
          over &&
          active.rect.current.translated &&
          active.rect.current.translated.top > over.rect.top + over.rect.height;
        const modifier = isBelowOverItem ? 1 : 0;
        newIndex = overIndex + modifier;
      } else {
        newIndex = overItems.length;
      }

      // Calculate Time
      const dropStartTime = getSmartDropTime(
        dragOverInfo,
        targetDayId,
        newIndex,
        overItems,
      );
      const dropEndTime = calculateEndTime(dropStartTime);

      // Create new item for optimistic update
      const tempId = `item-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      const newItem = {
        ...activeItem,
        id: tempId,
        type: "item",
        startTime: dropStartTime,
        endTime: dropEndTime,
        orderIndex: newIndex,
      };

      // Optimistic Update
      const prevItems = { ...items };
      setItems((prev) => ({
        ...prev,
        ideas: prev.ideas.filter((i) => i.id !== activeId),
        [targetDayId]: [
          ...prev[targetDayId].slice(0, newIndex),
          newItem,
          ...prev[targetDayId].slice(newIndex),
        ],
      }));

      // API Call
      const tripDayId = tripDayIdMap[targetDayId];
      if (tripDayId && activeItem.locationId) {
        try {
          const response = await tripLocationApi.add({
            tripDayId: tripDayId,
            locationId: activeItem.locationId,
            orderIndex: newIndex,
            startTime: dropStartTime + ":00",
            endTime: dropEndTime + ":00",
            note: null,
            transportMode: null,
          });

          const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
          const data = response?.value || response?.data || response;

          if (isApiSuccess && data) {
            // Update temp ID with real ID
            setItems((prev) => ({
              ...prev,
              [targetDayId]: prev[targetDayId].map((item) =>
                item.id === tempId
                  ? { ...item, tripLocationId: data.id || data.tripLocationId }
                  : item,
              ),
            }));
            toast.success(`Đã thêm "${activeItem.content}" vào lịch trình`);
          } else {
            setItems(prevItems);
            toast.error("Không thể thêm địa điểm vào lịch trình");
          }
        } catch (err) {
          console.error("Failed to add location to trip:", err);
          setItems(prevItems);

          if (err.response?.status === 403) {
            toast.error("Bạn không có quyền chỉnh sửa lịch trình này");
          } else {
            toast.error("Lỗi khi thêm địa điểm");
          }
        }
      } else {
        // Fallback if no API (development mode or missing data)
        toast.success(`Đã thêm "${activeItem.content}" vào lịch trình`);
      }
    },
    [items, setItems, tripDayIdMap],
  );

  return { handleIdeasToScheduleDrop };
}
