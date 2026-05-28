import React, { useState, useCallback, useEffect, useMemo } from "react";
import { BlogBlockEditorContext } from "./Context";

/**
 * BlogBlockEditor Provider
 * Manages the state and actions for the block-based blog editor.
 * Vercel Rules: rerender-memo, rerender-functional-setstate, rerender-callback-dependencies
 */
export const BlogBlockEditorProvider = ({
  children,
  initialBlocks = [],
  tripData = null,
  onChange,
}) => {
  const [blocks, setBlocks] = useState([]);

  // Vercel Rule: advanced-event-handler-refs
  const onChangeRef = React.useRef(onChange);
  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  // Vercel Rule: rerender-move-effect-to-event
  const setBlocksAndNotify = useCallback((updater) => {
    setBlocks((prev) => {
      const next = typeof updater === "function" ? updater(prev) : updater;
      queueMicrotask(() => {
        if (onChangeRef.current) onChangeRef.current(next);
      });
      return next;
    });
  }, []);

  const generateId = useCallback(() => {
    if (typeof crypto !== "undefined" && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    return Math.random().toString(36).substring(2, 11); // Fallback
  }, []);

  // Initialize blocks if provided
  useEffect(() => {
    if (initialBlocks.length > 0 && blocks.length === 0) {
      const blocksWithIds = initialBlocks
        .map((b) => ({
          ...b,
          id: b.id || generateId(), // Ensure every block has a client-side unique ID
          blockType: b.blockType || "location", // Default to location for legacy
        }))
        .sort((a, b) => a.displayOrder - b.displayOrder);
      setBlocksAndNotify(blocksWithIds);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialBlocks, generateId, setBlocksAndNotify]);

  // Sync with parent logic explicitly moved to setBlocksAndNotify

  // Actions
  const addBlock = useCallback(
    (type, index, data = {}) => {
      setBlocksAndNotify((prev) => {
        const newBlock = {
          id: generateId(),
          blockType: type,
          title: "",
          content: "",
          displayOrder: 0,
          ...data,
        };

        const newBlocks = [...prev];
        if (index !== undefined) {
          newBlocks.splice(index, 0, newBlock);
        } else {
          newBlocks.push(newBlock);
        }

        // Update displayOrder for all blocks
        return newBlocks.map((b, i) => ({ ...b, displayOrder: i }));
      });
    },
    [generateId],
  );

  const removeBlock = useCallback((id) => {
    setBlocksAndNotify((prev) => {
      const filtered = prev.filter((b) => b.id !== id);
      return filtered.map((b, i) => ({ ...b, displayOrder: i }));
    });
  }, []);

  const updateBlock = useCallback((id, updates) => {
    setBlocksAndNotify((prev) =>
      prev.map((b) => (b.id === id ? { ...b, ...updates } : b)),
    );
  }, []);

  const reorderBlocks = useCallback((startIndex, endIndex) => {
    setBlocksAndNotify((prev) => {
      const result = Array.from(prev);
      const [removed] = result.splice(startIndex, 1);
      result.splice(endIndex, 0, removed);

      return result.map((b, i) => ({ ...b, displayOrder: i }));
    });
  }, []);

  // Helpers to auto-generate from trip
  const generateFromTrip = useCallback(
    (trip) => {
      // The trip object from useTripDetails contains an array of tripDays
      if (!trip?.tripDays || trip.tripDays.length === 0) return;

      const newBlocks = [];

      // Sort days by dayIndex to ensure correct order
      const sortedDays = [...trip.tripDays].sort(
        (a, b) => a.dayIndex - b.dayIndex,
      );

      sortedDays.forEach((day) => {
        // Skip ideas day (dayIndex 0) if it has no locations or if you prefer to ignore ideas
        if (day.dayIndex === 0) return;

        // Skip days with no locations
        if (!day.locations || day.locations.length === 0) return;

        const dayNum = day.dayIndex;

        // Add Day Header
        newBlocks.push({
          id: generateId(),
          blockType: "day_header",
          dayNumber: Number(dayNum),
          title: day.title || `Ngày ${dayNum}`,
          content: "",
          displayOrder: newBlocks.length,
        });

        // Add Location Blocks
        day.locations
          .sort((a, b) => a.orderIndex - b.orderIndex)
          .forEach((loc) => {
            // Safely parse images
            let parsedImages = [];
            try {
              if (loc.location?.locationDetail?.images) {
                parsedImages = JSON.parse(loc.location.locationDetail.images);
              } else if (typeof loc.location?.images === "string") {
                parsedImages = JSON.parse(loc.location.images);
              } else if (Array.isArray(loc.location?.images)) {
                parsedImages = loc.location.images;
              }
            } catch (e) {
              console.warn("Failed to parse images for location", loc, e);
            }

            const getImageUrl = (img) => {
              if (!img) return "";
              if (typeof img === "string") return img;
              return img.url || img.imageUrl || img.src || "";
            };

            const images = parsedImages.length > 0
              ? parsedImages.map(getImageUrl).filter(Boolean)
              : loc.location?.imageUrl ? [loc.location.imageUrl] : [];

            const firstImage = images.length > 0 ? images[0] : "";

            // Format time range safely
            const timeRange =
              loc.startTime && loc.endTime
                ? `${loc.startTime.substring(0, 5)} - ${loc.endTime.substring(0, 5)}`
                : null;

            newBlocks.push({
              id: generateId(),
              blockType: "location",
              dayNumber: Number(dayNum),
              locationId: loc.locationId,
              destinationName: loc.location?.name,
              content: loc.note || "", // Pick up note as initial content
              images: images,
              imageUrl: firstImage,
              rating: loc.location?.ratingAverage || 0,
              categoryName: loc.location?.category?.name || "",
              address: loc.location?.address || "",
              timeRange: timeRange,
              displayOrder: newBlocks.length,
            });
          });
      });

      setBlocksAndNotify(newBlocks);
    },
    [generateId, setBlocksAndNotify],
  );

  const value = useMemo(
    () => ({
      blocks,
      tripData,
      addBlock,
      removeBlock,
      updateBlock,
      reorderBlocks,
      generateFromTrip,
      setBlocks: setBlocksAndNotify,
      generateId,
    }),
    [
      blocks,
      tripData,
      addBlock,
      removeBlock,
      updateBlock,
      reorderBlocks,
      generateFromTrip,
      setBlocksAndNotify,
      generateId,
    ],
  );

  return (
    <BlogBlockEditorContext.Provider value={value}>
      {children}
    </BlogBlockEditorContext.Provider>
  );
};
