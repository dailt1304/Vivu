import React from "react";
import { DndContext, closestCenter, DragOverlay } from "@dnd-kit/core";
import { useTripItineraryContext } from "./TripItineraryContext";

/**
 * TripItineraryFrame
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Wrapper component that provides DndContext for drag-and-drop functionality.
 * Consumes context for handlers and sensors.
 */
function TripItineraryFrame({ children }) {
  const { actions, meta } = useTripItineraryContext();
  const { handleDragStart, handleDragOver, handleDragEnd } = actions;
  const { sensors } = meta;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
    >
      {children}
    </DndContext>
  );
}

export default React.memo(TripItineraryFrame);
