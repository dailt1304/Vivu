/**
 * TripDetailsDrawer - Compound Component Structure
 *
 * This folder contains the decomposed TripDetailsDrawer component
 * following the Compound Components pattern for better maintainability.
 *
 * Components:
 * - TripDetailsContext: Shared state provider
 * - TripDrawerTabs: Tab navigation
 * - TripInfoHeader: Title, date, trip size editing
 * - TripDrawerTopBar: Close, share, expand buttons
 *
 * Hooks:
 * - useTripItinerary: State management for itinerary items
 *
 * Usage:
 * ```jsx
 * import TripDetailsDrawer from "./TripDetailsDrawer";
 * // or import individual components:
 * import { TripDrawerTabs, TripInfoHeader } from "./TripDetailsDrawer";
 * ```
 */

// Re-export the original component for backward compatibility
export { default } from "./TripDetailsDrawer.jsx";

// Export sub-components
export { TripDetailsProvider, useTripDetails } from "./TripDetailsContext";
export { default as TripDrawerTabs } from "./TripDrawerTabs";
export { TRIP_TABS } from "./constants";
export { default as TripInfoHeader } from "./TripInfoHeader";
export { default as TripDrawerTopBar } from "./TripDrawerTopBar";

// Export hooks
export { useTripItinerary } from "./useTripItinerary";
export { useDragHandlers } from "./useDragHandlers";
