/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext } from "react";

/**
 * TripItineraryContext
 *
 * Following Vercel Composition Pattern: state-context-interface
 * Generic interface with {state, actions, meta} for dependency injection
 *
 * This context provides:
 * - state: Current itinerary data (items, activeId, dragOverInfo)
 * - actions: Functions to modify state (setItems, drag handlers)
 * - meta: Non-reactive data (refs, sensors, dayIds, tripDayIdMap)
 */

// ============================================
// State Interface
// ============================================

/**
 * @typedef {Object} ItineraryItem
 * @property {string} id - Unique identifier
 * @property {string} content - Display content (location name)
 * @property {string} [locationId] - Location ID from backend
 * @property {string} [tripLocationId] - TripLocation ID from backend (for scheduled items)
 * @property {string} [startTime] - Start time in HH:mm format
 * @property {string} [endTime] - End time in HH:mm format
 * @property {'idea'|'item'} type - Item type
 */

/**
 * @typedef {Object} DragOverInfo
 * @property {string|null} dayId - Target day container ID
 * @property {number|null} index - Drop index position
 * @property {string|null} time - Calculated drop time (HH:mm)
 */

/**
 * @typedef {Object} TripItineraryState
 * @property {Record<string, ItineraryItem[]>} items - Items by container: { ideas: [], "day-1": [], ... }
 * @property {string|null} activeId - Currently dragging item ID
 * @property {DragOverInfo} dragOverInfo - Drop position indicator data
 * @property {string|null} currentOverContainer - Container being hovered during drag
 */

// ============================================
// Actions Interface
// ============================================

/**
 * @typedef {Object} TripItineraryActions
 * @property {Function} setItems - State setter for items
 * @property {Function} handleDragStart - DnD start handler
 * @property {Function} handleDragOver - DnD over handler
 * @property {Function} handleDragEnd - DnD end handler
 */

// ============================================
// Meta Interface (non-reactive)
// ============================================

/**
 * @typedef {Object} TripItineraryMeta
 * @property {Record<string, string>} tripDayIdMap - Mapping of dayKey to tripDayId
 * @property {string[]} dayIds - Array of day container IDs ["day-1", "day-2", ...]
 * @property {Object} sensors - DnD sensors configuration
 * @property {Object} ideasRef - Ref for ideas droppable container
 * @property {Function} onItemHover - Callback when hovering item
 * @property {Function} onItemLeave - Callback when leaving item
 */

// ============================================
// Context Value Interface
// ============================================

/**
 * @typedef {Object} TripItineraryContextValue
 * @property {TripItineraryState} state
 * @property {TripItineraryActions} actions
 * @property {TripItineraryMeta} meta
 */

/** @type {import('react').Context<TripItineraryContextValue | null>} */
const TripItineraryContext = createContext(null);

/**
 * useTripItineraryContext - Hook to access TripItinerary context
 *
 * @returns {TripItineraryContextValue}
 * @throws {Error} If used outside of TripItineraryProvider
 */
export function useTripItineraryContext() {
  const context = useContext(TripItineraryContext);
  if (!context) {
    throw new Error(
      "useTripItineraryContext must be used within TripItineraryProvider",
    );
  }
  return context;
}

/**
 * Selectors for optimized access (prevents unnecessary re-renders)
 */
export function useTripItineraryState() {
  const { state } = useTripItineraryContext();
  return state;
}

export function useTripItineraryActions() {
  const { actions } = useTripItineraryContext();
  return actions;
}

export function useTripItineraryMeta() {
  const { meta } = useTripItineraryContext();
  return meta;
}

export { TripItineraryContext };
export default TripItineraryContext;
