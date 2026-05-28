import React, { useState, useMemo, useEffect } from "react";
import { useSensor, useSensors, PointerSensor } from "@dnd-kit/core";
import { TripItineraryContext } from "./TripItineraryContext";
import { useDragHandlers } from "../TripDetailsDrawer/useDragHandlers";

const dropAnimation = {
  duration: 250,
  easing: "cubic-bezier(0.18, 0.67, 0.6, 1.22)",
};

/**
 * TripItineraryProvider
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Provider component that manages all DnD state and provides it via context.
 * Supports both controlled mode (external items/setItems) and uncontrolled mode.
 *
 * @param {Object} props
 * @param {Object} props.tripData - Trip data from API
 * @param {Record<string, string>} props.tripDayIdMap - Mapping of dayKey to tripDayId
 * @param {string[]} props.dayIds - Array of day container IDs
 * @param {Object} [props.items] - External items state (controlled mode)
 * @param {Function} [props.setItems] - External setItems function (controlled mode)
 * @param {Function} props.onItemHover - Callback when hovering item
 * @param {Function} props.onItemLeave - Callback when leaving item
 * @param {React.ReactNode} props.children - Child components
 */
function TripItineraryProvider({
  tripData,
  tripDayIdMap,
  dayIds,
  items: externalItems,
  setItems: externalSetItems,
  onItemHover,
  onItemLeave,
  canEdit = false,
  onReorderSuccess,
  children,
}) {
  // ============================================
  // State - Support both controlled and uncontrolled modes
  // ============================================

  // Internal items state (used only if external items not provided)
  const [internalItems, setInternalItems] = useState(() => {
    const initialItems = { ideas: [] };
    dayIds.forEach((dayId) => {
      initialItems[dayId] = [];
    });
    return initialItems;
  });

  // Use external items if provided, otherwise use internal
  const items = externalItems || internalItems;
  const setItems = externalSetItems || setInternalItems;

  // Sync internal items from tripData when it changes (only in uncontrolled mode)
  /* eslint-disable */
  useEffect(() => {
    if (externalItems) return; // Skip if controlled mode
    if (!tripData) return;

    const newItems = { ideas: [] };

    dayIds.forEach((dayId) => {
      newItems[dayId] = [];
    });

    // Process trip days
    if (tripData.tripDays) {
      tripData.tripDays.forEach((day, index) => {
        const dayId = `day-${index + 1}`;
        if (day.tripLocations) {
          newItems[dayId] = day.tripLocations.map((loc) => ({
            id: `loc-${loc.tripLocationId || loc.id}`,
            content: loc.location?.name || loc.name || "Unknown",
            type: "item",
            image: loc.location?.image || loc.image,
            category: loc.location?.category || loc.category,
            startTime: loc.startTime,
            endTime: loc.endTime,
            tripLocationId: loc.tripLocationId || loc.id,
            locationId: loc.location?.id || loc.locationId,
            latitude: loc.location?.latitude || loc.latitude,
            longitude: loc.location?.longitude || loc.longitude,
            orderIndex: loc.orderIndex,
          }));
        }
      });
    }

    setInternalItems(newItems);
  }, [tripData, dayIds, externalItems]);
  /* eslint-enable */

  // ============================================
  // DnD Handlers
  // ============================================

  const {
    activeId,
    dragOverInfo,
    currentOverContainer,
    handleDragStart,
    handleDragOver,
    handleDragEnd,
    findContainer,
  } = useDragHandlers({
    items,
    setItems,
    tripDayIdMap,
    dayIds,
    onReorderSuccess,
  });

  // ============================================
  // Sensors
  // ============================================

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // 8px movement before drag starts
      },
    }),
  );

  // ============================================
  // Context Value
  // ============================================

  const contextValue = useMemo(
    () => ({
      // State
      state: {
        items,
        activeId,
        dragOverInfo,
        currentOverContainer,
        canEdit,
      },
      // Actions
      actions: {
        setItems,
        handleDragStart,
        handleDragOver,
        handleDragEnd,
        findContainer,
      },
      // Meta (non-reactive)
      meta: {
        tripDayIdMap,
        dayIds,
        sensors,
        dropAnimation,
        onItemHover,
        onItemLeave,
        findContainer, // For IdeasPanel to detect drag from schedule
      },
    }),
    [
      items,
      activeId,
      dragOverInfo,
      currentOverContainer,
      canEdit,
      tripDayIdMap,
      dayIds,
      sensors,
      // dropAnimation, // Constant now
      onItemHover,
      onItemLeave,
      handleDragStart,
      handleDragOver,
      handleDragEnd,
      findContainer,
      setItems,
    ],
  );

  return (
    <TripItineraryContext.Provider value={contextValue}>
      {children}
    </TripItineraryContext.Provider>
  );
}

import PropTypes from "prop-types";

// ... existing code ...

TripItineraryProvider.propTypes = {
  tripData: PropTypes.object,
  tripDayIdMap: PropTypes.object,
  dayIds: PropTypes.arrayOf(PropTypes.string).isRequired,
  items: PropTypes.shape({
    ideas: PropTypes.array,
  }),
  setItems: PropTypes.func,
  onItemHover: PropTypes.func,
  onItemLeave: PropTypes.func,
  onReorderSuccess: PropTypes.func,
  children: PropTypes.node,
};

export default React.memo(TripItineraryProvider);
