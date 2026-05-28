import { useCallback } from "react";
import tripLocationApi from "../../../../../api/tripLocationApi";
import toast from "../../../../../utils/toast";

/**
 * Hook to handle moving items from Schedule back to Ideas (Remove from Trip)
 */
export function useScheduleToIdeas({ items, setItems }) {
  const handleScheduleToIdeasDrop = useCallback(
    async (active, activeContainer) => {
      const activeId = active.id;

      const activeItems = items[activeContainer];
      const activeItem = activeItems.find((i) => i.id === activeId);

      if (!activeItem) return;

      // Create new idea object
      const newItem = {
        ...activeItem,
        id: `idea-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        type: "idea",
        startTime: undefined,
        endTime: undefined,
        tripLocationId: undefined,
      };

      // Optimistic Update
      const prevItems = { ...items };
      setItems((prev) => ({
        ...prev,
        [activeContainer]: prev[activeContainer].filter(
          (i) => i.id !== activeId,
        ),
        ideas: [newItem, ...prev.ideas],
      }));

      // API Call
      if (activeItem.tripLocationId) {
        try {
          const response = await tripLocationApi.remove(
            activeItem.tripLocationId,
          );

          const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
          if (isApiSuccess) {
            toast.success(`Đã xóa "${activeItem.content}" khỏi lịch trình`);
          } else {
            // Revert on failure
            setItems(prevItems);
            toast.error("Không thể xóa địa điểm");
          }
        } catch (err) {
          console.error("Failed to remove location from trip:", err);
          setItems(prevItems);
          toast.error("Lỗi khi xóa địa điểm");
        }
      }
    },
    [items, setItems],
  );

  return { handleScheduleToIdeasDrop };
}
