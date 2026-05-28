import React from "react";
import { Skeleton } from "./skeleton";

/**
 * Skeleton loader for a Trip Card, mimicking the layout in Profile tabs.
 */
export const TripCardSkeleton = () => {
  return (
    <div className="relative rounded-2xl overflow-hidden shadow-md h-48">
      {/* Background Image Area */}
      <Skeleton className="h-full w-full rounded-none" />
      
      {/* Content Overlay */}
      <div className="absolute bottom-0 left-0 right-0 p-4 bg-linear-to-t from-black/60 to-transparent">
        {/* Title line */}
        <Skeleton className="h-6 w-3/4 mb-2 bg-white/30" />
        
        {/* Footer row */}
        <div className="flex items-center justify-between">
          <Skeleton className="h-3 w-1/4 bg-white/30" />
          <Skeleton className="h-3 w-1/5 bg-white/30" />
        </div>
      </div>
    </div>
  );
};

/**
 * List of Trip Card skeletons for grid layouts
 */
export const TripGridSkeleton = ({ count = 3 }) => {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <TripCardSkeleton key={i} />
      ))}
    </>
  );
};
