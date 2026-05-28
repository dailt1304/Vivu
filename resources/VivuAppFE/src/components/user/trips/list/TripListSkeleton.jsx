import React, { memo } from "react";
import { motion } from "framer-motion";

// Vercel Standard: Isolate perpetual animations into memoized components to prevent layout thrashing
const Shimmer = memo(() => (
  <motion.div
    className="absolute inset-0 z-50 bg-gradient-to-r from-transparent via-white/50 to-transparent pointer-events-none"
    initial={{ x: "-100%" }}
    animate={{ x: "100%" }}
    transition={{ repeat: Infinity, duration: 1.5, ease: "linear" }}
  />
));

const TripCardSkeleton = () => {
  return (
    <div className="relative h-64 rounded-3xl overflow-hidden bg-slate-50 ring-1 ring-slate-200 shadow-sm">
      <Shimmer />

      {/* Base Image Placeholder */}
      <div className="absolute inset-0 bg-slate-200/50" />

      {/* Status Badge Skeleton */}
      <div className="absolute top-4 left-4 z-20">
        <div className="w-28 h-7 bg-slate-300/80 rounded-full backdrop-blur-sm"></div>
      </div>

      {/* Content Overlay */}
      <div className="absolute bottom-0 left-0 right-0 p-6 z-20">
        {/* Meta Row Skeleton */}
        <div className="flex items-center gap-3 mb-3">
          <div className="w-20 h-4 bg-slate-300/80 rounded backdrop-blur-sm"></div>
          <div className="w-2 h-2 bg-slate-300/80 rounded-full backdrop-blur-sm"></div>
          <div className="w-16 h-4 bg-slate-300/80 rounded backdrop-blur-sm"></div>
        </div>

        {/* Title Skeleton */}
        <div className="space-y-2 mb-4">
          <div className="w-4/5 h-6 bg-slate-300/80 rounded backdrop-blur-sm"></div>
          <div className="w-3/5 h-6 bg-slate-300/80 rounded backdrop-blur-sm"></div>
        </div>

        {/* Date Badge Skeleton */}
        <div className="w-40 h-10 bg-slate-300/80 rounded-xl backdrop-blur-sm"></div>
      </div>
    </div>
  );
};

const TripListSkeleton = ({ count = 4 }) => {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6 pb-12">
      {Array.from({ length: count }).map((_, idx) => (
        <TripCardSkeleton key={idx} />
      ))}
    </div>
  );
};

export default TripListSkeleton;
