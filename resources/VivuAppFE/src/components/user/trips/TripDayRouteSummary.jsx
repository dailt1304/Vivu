import React from "react";
import { Car, Clock, MapPin, Navigation } from "lucide-react";

// Vercel Rule: rendering-hoist-jsx - Static display helpers
const formatDistance = (meters) => {
  if (meters < 1000) return `${meters} m`;
  return `${(meters / 1000).toFixed(1)} km`;
};

const formatDuration = (seconds) => {
  if (seconds < 60) return `${seconds} giây`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes} phút`;
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return `${hours} giờ ${remainingMinutes > 0 ? `${remainingMinutes} phút` : ""}`;
};

const TripDayRouteSummary = ({ routeData, isLoading, stopCount }) => {
  if (stopCount < 2) return null;

  if (isLoading) {
    return (
      <div className="bg-white/90 backdrop-blur shadow-sm border border-gray-100 rounded-2xl p-3 flex gap-4 items-center justify-center animate-pulse">
        <div className="h-4 bg-gray-200 rounded w-24"></div>
        <div className="w-1 h-1 bg-gray-300 rounded-full"></div>
        <div className="h-4 bg-gray-200 rounded w-24"></div>
        <div className="w-1 h-1 bg-gray-300 rounded-full"></div>
        <div className="h-4 bg-gray-200 rounded w-24"></div>
      </div>
    );
  }

  if (!routeData) return null;

  return (
    <div className="bg-white/90 backdrop-blur shadow-md shadow-blue-900/5 border border-blue-100 rounded-2xl px-4 py-2.5 flex flex-wrap gap-x-5 gap-y-2 items-center justify-center text-sm">
      <div className="flex items-center gap-1.5 text-gray-700">
        <MapPin size={16} className="text-red-500" />
        <span className="font-medium">{stopCount} điểm dừng</span>
      </div>

      <div className="w-1.5 h-1.5 rounded-full bg-gray-300 hidden sm:block"></div>

      <div className="flex items-center gap-1.5 text-gray-700">
        <Navigation size={15} className="text-blue-500" />
        <span>
          Tổng:{" "}
          <b className="text-gray-900">
            {formatDistance(routeData.totalDistance)}
          </b>
        </span>
      </div>

      <div className="w-1.5 h-1.5 rounded-full bg-gray-300 hidden sm:block"></div>

      <div className="flex items-center gap-1.5 text-gray-700">
        <Clock size={15} className="text-amber-500" />
        <span>
          Di chuyển:{" "}
          <b className="text-gray-900">
            {formatDuration(routeData.totalDuration)}
          </b>
        </span>
      </div>
    </div>
  );
};

// Vercel Rule: rerender-memo
export default React.memo(TripDayRouteSummary);
