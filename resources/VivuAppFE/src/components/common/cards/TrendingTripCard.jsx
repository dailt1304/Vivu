import React, { memo } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { User, MapPin, Heart, Calendar } from "lucide-react";

/**
 * TrendingTripCard component for displaying top trips in a horizontal carousel.
 * Vercel Rules Applied:
 * - rerender-memo: Memoized to prevent unnecessary re-renders in carousels
 * - bundle-preload: Preloads target on hover
 * - rendering-hoist-jsx: Static structure
 */
const TrendingTripCard = memo(({ trip, className = "" }) => {
  const navigate = useNavigate();

  const handleMouseEnter = () => {
    import("../../../pages/user/trips/TripDetailPage");
  };

  const handleClick = () => {
    navigate(`/trips/${trip.id}`);
  };

  if (!trip) return null;

  return (
    <motion.div
      whileHover={{ y: -5 }}
      onClick={handleClick}
      onMouseEnter={handleMouseEnter}
      className={`group relative rounded-3xl overflow-hidden cursor-pointer shadow-md hover:shadow-2xl transition-all duration-300 h-[300px] w-[260px] sm:w-[280px] shrink-0 ${className}`}
    >
      {/* Background Image */}
      <img
        src={trip.coverUrl}
        alt={trip.title}
        className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-110"
      />

      {/* Gradient Overlay */}
      <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/30 to-black/10 opacity-90 group-hover:opacity-95 transition-opacity" />

      {/* Top Badge (Owner) */}
      <div className="absolute top-4 left-4">
        <div className="bg-black/30 backdrop-blur-md border border-white/10 px-3 py-1.5 rounded-full text-xs font-medium text-white flex items-center gap-1.5">
          <div className="w-4 h-4 rounded-full overflow-hidden bg-white/20">
            {trip.ownerAvatar ? (
              <img
                src={trip.ownerAvatar}
                alt=""
                className="w-full h-full object-cover"
              />
            ) : (
              <User size={10} className="w-full h-full text-white/70 p-0.5" />
            )}
          </div>
          <span className="truncate max-w-[100px]">{trip.ownerName}</span>
        </div>
      </div>

      {/* Floating Heart Badge */}
      <div className="absolute top-4 right-4 bg-white/10 backdrop-blur-md border border-white/20 px-2.5 py-1 rounded-full text-[10px] font-bold text-white flex items-center gap-1">
        <Heart size={10} className="text-red-400 fill-red-400" />
        {trip.favoritesCount}
      </div>

      {/* Bottom Content */}
      <div className="absolute bottom-0 left-0 p-5 w-full flex flex-col gap-2">
        {/* City and Duration Tags */}
        <div className="flex items-center gap-2 flex-wrap">
          {trip.cityName && (
            <span className="bg-indigo-600/80 backdrop-blur-sm text-[10px] uppercase font-bold text-white px-2 py-0.5 rounded flex items-center gap-1">
              <MapPin size={10} />
              {trip.cityName}
            </span>
          )}
          {trip.durationDays && (
            <span className="bg-white/20 backdrop-blur-sm text-[10px] uppercase font-bold text-indigo-100 px-2 py-0.5 rounded flex items-center gap-1">
              <Calendar size={10} />
              {trip.durationDays} ngày
            </span>
          )}
        </div>

        {/* Title */}
        <h3 className="text-xl font-bold text-white leading-tight line-clamp-2 drop-shadow-sm group-hover:text-indigo-200 transition-colors">
          {trip.title}
        </h3>
      </div>
    </motion.div>
  );
});

TrendingTripCard.displayName = "TrendingTripCard";

export default TrendingTripCard;
