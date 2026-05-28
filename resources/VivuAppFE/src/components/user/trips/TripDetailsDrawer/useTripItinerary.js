import { useState, useEffect, useRef, useCallback } from "react";
import tripLocationApi from "../../../../api/tripLocationApi";
import toast from "../../../../utils/toast";

const START_HOUR = 6;
const END_HOUR = 23;
const TOTAL_DAY_MINUTES = (END_HOUR - START_HOUR) * 60;

/**
 * useTripItinerary - Custom hook for managing trip itinerary state
 *
 * Extracts the core state management from TripDetailsDrawer for:
 * - Items (ideas + day schedules)
 * - Trip day ID mapping
 * - CRUD operations on trip locations
 */
export function useTripItinerary(tripData) {
  // Initial items state with sample ideas
  const [items, setItems] = useState({
    ideas: [],
    "day-1": [],
    "day-2": [],
    "day-3": [],
  });

  // Track if we've initialized from tripData
  const [hasInitialized, setHasInitialized] = useState(false);

  // Map day keys to actual tripDayIds for API calls
  const [tripDayIdMap, setTripDayIdMap] = useState({});

  // Track the previous tripDays count
  const prevTripDaysCountRef = useRef(0);
  const itemsRef = useRef(items);

  // Keep itemsRef in sync
  useEffect(() => {
    itemsRef.current = items;
  }, [items]);

  // Initialize items from tripData
  useEffect(() => {
    const currentCount = tripData?.tripDays?.length || 0;
    const shouldReinitialize =
      !hasInitialized ||
      (currentCount !== prevTripDaysCountRef.current &&
        prevTripDaysCountRef.current > 0);

    if (tripData?.tripDays && shouldReinitialize) {
      const currentItems = itemsRef.current;
      const existingIdeas = hasInitialized ? currentItems.ideas : [];
      const newItems = { ideas: existingIdeas };
      const dayIdMap = {};

      tripData.tripDays.forEach((day, dayIdx) => {
        const dayKey = `day-${dayIdx + 1}`;
        dayIdMap[dayKey] = day.id;

        if (hasInitialized && currentItems[dayKey]) {
          newItems[dayKey] = currentItems[dayKey];
        } else {
          newItems[dayKey] = (day.locations || []).map((loc, locIdx) => ({
            id: `item-${day.dayIndex || dayIdx + 1}-${locIdx}`,
            tripLocationId: loc.id,
            content: loc.location?.name || `Địa điểm ${locIdx + 1}`,
            type: "item",
            category: loc.location?.category || "Attraction",
            image:
              loc.location?.imageUrl ||
              "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?q=80&w=400&auto=format&fit=crop",
            latitude: loc.location?.latitude,
            longitude: loc.location?.longitude,
            startTime: loc.startTime?.slice(0, 5) || "09:00",
            endTime: loc.endTime?.slice(0, 5) || "11:00",
            note: loc.note || loc.location?.description || "",
            locationId: loc.locationId || loc.location?.id,
            orderIndex: loc.orderIndex || locIdx,
          }));
        }
      });

      // Batch state updates to avoid cascading renders
      // Using refs and single setState pattern
      prevTripDaysCountRef.current = currentCount;

      // Update all states together
      setItems(newItems);
      setTripDayIdMap(dayIdMap);
      if (!hasInitialized) {
        setHasInitialized(true);
      }
    }
  }, [tripData, hasInitialized]);

  // Find which container an item belongs to
  const findContainer = useCallback(
    (id) => {
      if (id in items) return id;
      return Object.keys(items).find(
        (key) => items[key].find((item) => item.id === id)?.id,
      );
    },
    [items],
  );

  // Handle time change for an item
  const handleTimeChange = useCallback((id, field, value) => {
    setItems((prev) => {
      const newItems = { ...prev };
      const dayKey = Object.keys(newItems).find(
        (k) => k.startsWith("day-") && newItems[k].some((i) => i.id === id),
      );

      if (dayKey) {
        const index = newItems[dayKey].findIndex((i) => i.id === id);
        if (index !== -1) {
          const currentItem = newItems[dayKey][index];
          let startTime = currentItem.startTime;
          let endTime = currentItem.endTime;

          if (field === "startTime") startTime = value;
          if (field === "endTime") endTime = value;

          // Validate times
          if (startTime && endTime) {
            const startMinutes =
              parseInt(startTime.split(":")[0]) * 60 +
              parseInt(startTime.split(":")[1]);
            const endMinutes =
              parseInt(endTime.split(":")[0]) * 60 +
              parseInt(endTime.split(":")[1]);

            if (endMinutes < startMinutes) {
              toast.error("Thời gian kết thúc phải sau thời gian bắt đầu");
              return prev;
            }
          }

          const updatedList = [...newItems[dayKey]];
          updatedList[index] = { ...updatedList[index], [field]: value };
          newItems[dayKey] = updatedList;
        }
      }
      return newItems;
    });
  }, []);

  // Add a new idea from location search
  const addIdea = useCallback((location) => {
    const newIdea = {
      id: `idea-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      content: location.name,
      type: "idea",
      image: location.image,
      category: location.category || "Attraction",
      locationId: location.id,
      latitude: location.latitude,
      longitude: location.longitude,
    };
    setItems((prev) => ({
      ...prev,
      ideas: [newIdea, ...prev.ideas],
    }));
    toast.success(`Đã thêm "${location.name}" vào Ý tưởng`);
    return newIdea;
  }, []);

  // Delete an item from a container
  const deleteItem = useCallback(
    async (itemId, containerId) => {
      const itemToDelete = items[containerId]?.find((i) => i.id === itemId);
      if (!itemToDelete) return false;

      // Optimistic update
      const prevItems = { ...items };
      setItems((prev) => ({
        ...prev,
        [containerId]: prev[containerId].filter((i) => i.id !== itemId),
      }));

      // If persisted, call API
      if (itemToDelete.tripLocationId) {
        try {
          const response = await tripLocationApi.remove(
            itemToDelete.tripLocationId,
          );
          const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
          if (isApiSuccess) {
            toast.success(`Đã xóa "${itemToDelete.content}" khỏi lịch trình`);
            return true;
          } else {
            setItems(prevItems);
            toast.error("Không thể xóa địa điểm");
            return false;
          }
        } catch (err) {
          console.error("Failed to delete location:", err);
          setItems(prevItems);
          toast.error("Lỗi khi xóa địa điểm");
          return false;
        }
      }
      return true;
    },
    [items],
  );

  return {
    items,
    setItems,
    tripDayIdMap,
    findContainer,
    handleTimeChange,
    addIdea,
    deleteItem,
    constants: { START_HOUR, END_HOUR, TOTAL_DAY_MINUTES },
  };
}

export default useTripItinerary;
