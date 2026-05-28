import { useCallback } from "react";
import tripLocationApi from "../../../../api/tripLocationApi";
import tripDayApi from "../../../../api/tripDayApi";
import toast from "../../../../utils/toast";

const DEFAULT_IDEA_IMAGE =
  "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?q=80&w=800&auto=format&fit=crop";

/**
 * Hook to manage Ideas (DayIndex 0) logic
 */
export const useIdeasManagement = ({
  tripId,
  items,
  setItems,
  tripDayIdMap,
  setTripDayIdMap,
  onFlyTo,
  canEdit,
  onTripUpdate,
}) => {
  /**
   * Helper to ensure Ideas Day (DayIndex 0) exists
   */
  const ensureIdeasDay = useCallback(async () => {
    let ideasDayId = tripDayIdMap["ideas"];

    if (!ideasDayId) {
      try {
        const response = await tripDayApi.add({
          tripId: tripId,
          title: "Ideas",
          dayDate: null, // Day 0 doesn't have a specific date
          dayIndex: 0, // CRITICAL: Tells backend this is the Ideas day
        });

        const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
        if (isApiSuccess) {
          const newDay = response?.value || response?.data || response;
          ideasDayId = newDay.id;

          setTripDayIdMap((prev) => ({
            ...prev,
            ideas: ideasDayId,
          }));

          return ideasDayId;
        }
      } catch (error) {
        console.error("Failed to create Ideas day:", error);
        return null;
      }
    }

    return ideasDayId;
  }, [tripId, tripDayIdMap, setTripDayIdMap]);

  /**
   * Add a new location to Ideas panel
   */
  const handleConfirmAddLocation = useCallback(
    async (location) => {
      if (!canEdit) return;

      // Prevent duplicate additions
      const alreadyExists = items.ideas.some(
        (idea) => idea.locationId === location.id,
      );
      if (alreadyExists) {
        toast.info(`"${location.name}" đã có trong Ý tưởng`);
        return;
      }

      const tempId = `idea-temp-${Date.now()}`;
      const newIdea = {
        id: tempId,
        content: location.name,
        type: "idea",
        image: location.image || DEFAULT_IDEA_IMAGE,
        category: location.category || "Attraction",
        locationId: location.id,
        latitude: location.latitude,
        longitude: location.longitude,
      };

      // Optimistic UI Update
      setItems((prev) => ({
        ...prev,
        ideas: [newIdea, ...prev.ideas],
      }));

      // Fly to location
      if (location.latitude && location.longitude) {
        onFlyTo?.({
          latitude: location.latitude,
          longitude: location.longitude,
          zoom: 14,
        });
      }

      // Persist to DB
      try {
        const ideasDayId = await ensureIdeasDay();
        if (!ideasDayId)
          throw new Error("Could not find or create Ideas DayId");

        const response = await tripLocationApi.add({
          tripDayId: ideasDayId,
          locationId: location.id,
          orderIndex: 0, // Ideas are usually prepend/append
          startTime: null,
          endTime: null,
          note: null,
          transportMode: null,
        });

        const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
        if (isApiSuccess) {
          const addedLoc = response?.value || response?.data || response;
          // Update item with real tripLocationId
          setItems((prev) => ({
            ...prev,
            ideas: prev.ideas.map((item) =>
              item.id === tempId
                ? { ...item, tripLocationId: addedLoc.id || addedLoc.tripLocationId }
                : item,
            ),
          }));
          toast.success(`Đã thêm "${location.name}" vào Ý tưởng`);
          onTripUpdate?.(false);
        } else {
          throw new Error("API call failed");
        }
      } catch (error) {
        console.error("Failed to persist idea:", error);
        // Revert optimistic update
        setItems((prev) => ({
          ...prev,
          ideas: prev.ideas.filter((i) => i.id !== tempId),
        }));
        toast.error(`Không thể lưu "${location.name}" vào máy chủ`);
      }
    },
    [canEdit, items.ideas, setItems, onFlyTo, ensureIdeasDay, onTripUpdate],
  );

  return {
    handleConfirmAddLocation,
    DEFAULT_IDEA_IMAGE,
  };
};
