import { useCallback } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import tripLocationApi from "../../../../../api/tripLocationApi";
import toast from "../../../../../utils/toast";

/**
 * Hook to handle reordering items within the same container
 */
export function useReorderItems({
  items,
  setItems,
  tripDayIdMap,
  onReorderSuccess,
}) {
  const handleReorder = useCallback(
    async (active, over, activeContainer) => {
      // If reordering in ideas, just update state (no API needed normally, or maybe specific API)
      if (activeContainer === "ideas") {
        const activeIndex = items.ideas.findIndex((i) => i.id === active.id);
        const overIndex = items.ideas.findIndex((i) => i.id === over?.id);

        if (activeIndex !== overIndex) {
          setItems((prev) => ({
            ...prev,
            ideas: arrayMove(prev.ideas, activeIndex, overIndex),
          }));
        }
        return;
      }

      // Reordering in Schedule (Day Column)
      const currentItems = items[activeContainer];
      const activeIndex = currentItems.findIndex((i) => i.id === active.id);
      const overIndex = currentItems.findIndex((i) => i.id === over?.id);

      if (activeIndex !== overIndex && overIndex >= 0) {
        const prevItems = { ...items };
        // Optimistic Update with time swapping
        const originalTimes = currentItems.map((item) => ({
          startTime: item.startTime,
          endTime: item.endTime,
        }));

        const newOrderedItems = arrayMove(
          currentItems,
          activeIndex,
          overIndex,
        ).map((item, idx) => ({
          ...item,
          startTime: originalTimes[idx].startTime,
          endTime: originalTimes[idx].endTime,
        }));

        setItems((prev) => ({
          ...prev,
          [activeContainer]: newOrderedItems,
        }));

        // API Call
        const tripDayId = tripDayIdMap[activeContainer];
        const orderedTripLocationIds = newOrderedItems
          .filter((item) => item.tripLocationId)
          .map((item) => item.tripLocationId);

        if (tripDayId && orderedTripLocationIds.length > 0) {
          try {
            const response = await tripLocationApi.reorder({
              tripDayId: tripDayId,
              OrderedTripLocationIds: orderedTripLocationIds,
            });

            const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
            if (isApiSuccess) {
              const activeItem = currentItems[activeIndex]; // Note: activeIndex is old index
              toast.success(
                `Đã cập nhật vị trí "${activeItem?.content || "địa điểm"}"`,
              );
              // Broadcast reorder to other trip members via SignalR
              onReorderSuccess?.();
            } else {
              setItems(prevItems);
              toast.error("Không thể sắp xếp lại địa điểm");
            }
          } catch (err) {
            console.error("Failed to reorder locations:", err);
            setItems(prevItems);

            if (err.response?.status === 403) {
              toast.error("Bạn không có quyền chỉnh sửa lịch trình này");
            } else {
              toast.error("Lỗi khi sắp xếp lại");
            }
          }
        } else {
          // Local update only (no API params available)
          const activeItem = currentItems[activeIndex];
          toast.success(
            `Đã cập nhật vị trí "${activeItem?.content || "địa điểm"}"`,
          );
        }
      }
    },
    [items, setItems, tripDayIdMap, onReorderSuccess],
  );

  return { handleReorder };
}
