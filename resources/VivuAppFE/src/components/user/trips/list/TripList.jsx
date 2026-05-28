import React, { lazy, Suspense } from "react";
import TripCard from "./TripCard";
import TripCardRating from "./TripCardRating";
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import { useGridColumns } from "../../../../hooks/utils/useGridColumns";

// Lazy load the full-width panel (Code Splitting)
const RatingExpandPanel = lazy(() => import("./RatingExpandPanel"));

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 },
  },
};

const TripList = ({ trips, onDelete, onRateSubmit, onToggleVisibility }) => {
  const navigate = useNavigate();
  const columns = useGridColumns();

  // State for expanded item
  const [expandedId, setExpandedId] = React.useState(null);

  const handleToggleExpand = React.useCallback((id) => {
    setExpandedId((prev) => (prev === id ? null : id));
  }, []);

  if (!trips || trips.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center text-center py-12">
        <span className="text-4xl mb-4">✈️</span>
        <h3 className="text-xl font-bold text-gray-900 mb-2">
          Chưa có chuyến đi nào
        </h3>
        <p className="text-gray-500 mb-6">
          Hãy bắt đầu lên kế hoạch cho hành trình mới!
        </p>
      </div>
    );
  }

  // Calculate where to insert the panel
  // It should be after the last item of the row containing the expanded item
  let insertIndex = -1;
  const expandedIndex = trips.findIndex((t) => t.id === expandedId);

  if (expandedIndex !== -1) {
    const rowNumber = Math.floor(expandedIndex / columns);
    // Last item of the current row
    insertIndex = (rowNumber + 1) * columns - 1;
    // Or last item of list if row is incomplete
    if (insertIndex >= trips.length) insertIndex = trips.length - 1;
  }

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 pb-12 items-start"
    >
      {trips.map((trip, index) => {
        const isCompleted = trip.status === "completed";
        const isExpanded = expandedId === trip.id;

        return (
          <React.Fragment key={trip.id}>
            {/* TRIP CARD ITEM */}
            <div className={`relative ${isExpanded ? "z-10" : "z-0"}`}>
              <TripCard
                {...trip}
                isExpanded={isExpanded} // highlight effect
                onDelete={onDelete}
                onToggleVisibility={onToggleVisibility}
                onClick={() => navigate(`/trips/${trip.id}`)}
              />

              {/* Always visible READ MODE / CTA (Hybrid Design) */}
              {isCompleted && (
                <TripCardRating
                  isRated={trip.isRated}
                  rating={trip.rating}
                  review={trip.review}
                  isOwner={trip.isOwner}
                  onClick={() => handleToggleExpand(trip.id)}
                />
              )}

              {/* Expand Triangle (Visual connector) */}
              {isExpanded && (
                <motion.div
                  layoutId="expand-arrow"
                  className="absolute bottom-[-34px] left-1/2 -translate-x-1/2 w-0 h-0 border-l-[12px] border-l-transparent border-r-[12px] border-r-transparent border-b-[12px] border-b-white dark:border-b-gray-800 z-20"
                />
              )}
            </div>

            {/* EXPAND PANEL (Inserted after the row) */}
            <AnimatePresence mode="wait">
              {index === insertIndex && expandedId ? (
                <Suspense
                  fallback={
                    <div className="col-span-full h-32 animate-pulse bg-gray-100 rounded-xl" />
                  }
                >
                  {trips.map((t) =>
                    t.id === expandedId ? (
                      <RatingExpandPanel
                        key="panel"
                        trip={t}
                        onSubmit={onRateSubmit}
                        onClose={() => setExpandedId(null)}
                      />
                    ) : null,
                  )}
                </Suspense>
              ) : null}
            </AnimatePresence>
          </React.Fragment>
        );
      })}
    </motion.div>
  );
};

export default TripList;
