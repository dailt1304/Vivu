import { useCallback } from "react";
import tripLocationApi from "../../../../../api/tripLocationApi";
import toast from "../../../../../utils/toast";
import {
  calculateEndTime,
  getSmartDropTime,
} from "../utils/dragTimeCalculator";

/**
 * Hook to handle moving items between different days
 */
export function useDayToDay({ items, setItems, tripDayIdMap }) {
  const handleDayToDayDrop = useCallback(
    async (active, over, activeContainer, overContainer, dragOverInfo) => {
      // Find active item in EITHER container (robustness)
      const activeItem =
        items[activeContainer]?.find((i) => i.id === active.id) ||
        items[overContainer]?.find((i) => i.id === active.id);

      if (!activeItem) return;

      const overItems = items[overContainer];
      const overIndex = over
        ? overItems.findIndex((i) => i.id === over.id)
        : overItems.length;

      // Calculate new index
      let newIndex;
      if (overIndex >= 0 && over && over.id !== overContainer) {
        const isBelowOverItem =
          over &&
          active.rect.current.translated &&
          active.rect.current.translated.top > over.rect.top + over.rect.height;
        const modifier = isBelowOverItem ? 1 : 0;
        newIndex = overIndex + modifier;
      } else {
        newIndex = overItems.length;
      }

      // Calculate Times
      const dropStartTime = getSmartDropTime(
        dragOverInfo,
        overContainer,
        newIndex,
        overItems,
      );
      const dropEndTime = calculateEndTime(dropStartTime);

      // Optimistic Update Prep
      const prevItems = JSON.parse(JSON.stringify(items));
      const originalItemId = active.id;

      // Update State: Remove from old, Add to new
      setItems((prev) => {
        const newActive = prev[activeContainer].filter(
          (i) => i.id !== active.id,
        );
        const newItem = {
          ...activeItem,
          startTime: dropStartTime,
          endTime: dropEndTime,
          orderIndex: newIndex,
        };

        return {
          ...prev,
          [activeContainer]: newActive,
          [overContainer]: [
            ...prev[overContainer].slice(0, newIndex),
            newItem,
            ...prev[overContainer].slice(newIndex),
          ],
        };
      });

      // API Calls
      const oldTripDayId = tripDayIdMap[activeContainer];
      const newTripDayId = tripDayIdMap[overContainer];

      if (oldTripDayId && newTripDayId && activeItem.tripLocationId) {
        try {
          // Step 1: Remove from old day
          const removeResponse = await tripLocationApi.remove(
            activeItem.tripLocationId,
          );

          const isRemoveSuccess = !removeResponse || (removeResponse.isSuccess !== false && removeResponse.success !== false);
          if (!isRemoveSuccess) {
            throw new Error("Failed to remove from old day");
          }

          // Step 2: Add to new day
          const addResponse = await tripLocationApi.add({
            tripDayId: newTripDayId,
            locationId: activeItem.locationId,
            orderIndex: newIndex,
            startTime: dropStartTime + ":00",
            endTime: dropEndTime + ":00",
            note: activeItem.note || null,
            transportMode: activeItem.transportMode || null,
          });

          const isAddSuccess = !addResponse || (addResponse.isSuccess !== false && addResponse.success !== false);
          const data = addResponse?.value || addResponse?.data || addResponse;

          if (isAddSuccess && data) {
            // Update with new ID
            setItems((prev) => ({
              ...prev,
              [overContainer]: prev[overContainer].map((item) =>
                item.id === originalItemId
                  ? { ...item, tripLocationId: data.id || data.tripLocationId }
                  : item,
              ),
            }));
            toast.success(
              `Đã di chuyển "${activeItem.content}" sang ngày khác`,
            );
          } else {
            throw new Error("Failed to add to new day");
          }
        } catch (err) {
          console.error("Failed to move location between days:", err);
          setItems(prevItems);

          if (err.response?.status === 403) {
            toast.error("Bạn không có quyền chỉnh sửa lịch trình này");
          } else {
            toast.error("Lỗi khi di chuyển địa điểm");
          }
        }
      } else if (!activeItem.tripLocationId) {
        // Local item only
        toast.success(`Đã di chuyển "${activeItem.content}" sang ngày khác`);
      } else {
        toast.error("Không thể di chuyển - thiếu thông tin ngày");
        setItems(prevItems);
      }
    },
    [items, setItems, tripDayIdMap],
  );

  return { handleDayToDayDrop };
}
