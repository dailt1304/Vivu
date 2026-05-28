/**
 * TripItinerary - Compound Component
 *
 * Following Vercel Composition Pattern: architecture-compound-components
 *
 * Usage:
 * ```jsx
 * <TripItinerary.Provider tripData={...} tripDayIdMap={...} dayIds={...}>
 *   <TripItinerary.Frame>
 *     <div className="flex">
 *       <TripItinerary.IdeasPanel />
 *       <TripItinerary.DayColumns dates={...} />
 *     </div>
 *     <TripItinerary.DragOverlay />
 *   </TripItinerary.Frame>
 * </TripItinerary.Provider>
 * ```
 */

// Context
export {
  TripItineraryContext,
  useTripItineraryContext,
  useTripItineraryState,
  useTripItineraryActions,
  useTripItineraryMeta,
} from "./TripItineraryContext";

// Provider
export { default as TripItineraryProvider } from "./TripItineraryProvider";

// Compound Components
import TripItineraryProvider from "./TripItineraryProvider";
import TripItineraryFrame from "./TripItineraryFrame";
import IdeasPanel from "./IdeasPanel";
import DayColumns from "./DayColumns";
import TripDragOverlay from "./TripDragOverlay";

/**
 * TripItinerary compound component namespace
 *
 * Provides a clean API for composing itinerary components:
 * - Provider: Manages state, actions, and meta
 * - Frame: DndContext wrapper
 * - IdeasPanel: Ideas list with drag support
 * - DayColumns: Day columns with scheduled items
 * - DragOverlay: Visual feedback during drag
 */
const TripItinerary = {
  Provider: TripItineraryProvider,
  Frame: TripItineraryFrame,
  IdeasPanel: IdeasPanel,
  DayColumns: DayColumns,
  DragOverlay: TripDragOverlay,
};

export default TripItinerary;
