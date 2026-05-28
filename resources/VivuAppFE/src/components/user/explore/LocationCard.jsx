import React from "react";
import { Star, MapPin, Navigation2, Plus } from "lucide-react";

import { motion } from "framer-motion";
import CategoryIcon from "../../common/CategoryIcon";

// Vercel Rule: rerender-memo - Hoist static animation config outside component
const cardAnimationConfig = {
  whileHover: { y: -4, scale: 1.02 },
  whileTap: { scale: 0.98 },
  transition: { type: "spring", stiffness: 100, damping: 20 },
};

const LocationCard = ({
  image,
  title,
  location,
  rating,
  reviews,
  category,
  categoryIcon,
  onAddToCollection,
  onClick,
  onMouseEnter,
  onMouseLeave,
  distanceInMeters,
  layout = "vertical",
}) => {
  // Vercel: rerender-derived-state — format distance inline
  const formattedDistance =
    distanceInMeters != null
      ? distanceInMeters >= 1000
        ? `${(distanceInMeters / 1000).toFixed(1)} km`
        : `${Math.round(distanceInMeters)} m`
      : null;

  const handleAddToCollection = (e) => {
    e.stopPropagation();
    onAddToCollection?.();
  };

  return (
    <motion.div
      onClick={onClick}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      {...cardAnimationConfig}
      className={`group flex bg-white/90 backdrop-blur-md rounded-2xl shadow-sm hover:shadow-[0_8px_30px_rgb(0,0,0,0.06)] ring-1 ring-slate-200/80 hover:ring-blue-500/30 overflow-hidden cursor-pointer transition-shadow duration-300 ${
        layout === "horizontal" ? "flex-row h-36" : "flex-col h-full"
      }`}
    >
      {/* Image Section */}
      <div
        className={`relative overflow-hidden shrink-0 ${
          layout === "horizontal"
            ? "w-[120px] sm:w-[140px] h-full"
            : "aspect-4/3 w-full"
        }`}
      >
        <img
          src={
            image ||
            "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=400&auto=format&fit=crop"
          }
          alt={title}
          loading="lazy"
          fetchpriority="low"
          decoding="async"
          onError={(e) => {
            e.target.onerror = null; // Prevent infinite loop
            e.target.src =
              "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=400&auto=format&fit=crop";
          }}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />

        {/* Distance Badge */}
        {formattedDistance && (
          <div className="absolute bottom-2 left-2 bg-gradient-primary backdrop-blur-sm text-white text-[10px] font-bold px-2 py-0.5 rounded-full flex items-center gap-1 shadow">
            <Navigation2 size={10} />
            {formattedDistance}
          </div>
        )}

        {/* Actions Container (Top Right) */}
        <div className="absolute top-2 right-2 flex gap-2">
          <button
            onClick={handleAddToCollection}
            className="p-2 rounded-full bg-white/80 backdrop-blur-sm hover:bg-white text-gray-600 hover:text-blue-500 transition-colors shadow-sm"
            title="Thêm vào bộ sưu tập"
          >
            <Plus size={18} className="stroke-[2.5]" />
          </button>
        </div>
      </div>

      {/* Content Section */}
      <div className="flex-1 p-3 flex flex-col gap-2 min-w-0 overflow-hidden">
        <div className="w-full">
          <div className="flex items-start justify-between gap-2 w-full">
            <h3
              className="text-base font-bold text-gray-900 group-hover:text-primary transition-colors flex-1 min-w-0 truncate"
              title={title}
            >
              {title}
            </h3>
            {rating ? (
              <div className="flex items-center gap-0.5 shrink-0 bg-yellow-50 px-1.5 py-0.5 rounded-md border border-yellow-100">
                <Star
                  size={12}
                  className="fill-yellow-400 text-yellow-400 shrink-0"
                />
                <span className="text-xs font-semibold text-gray-800">
                  {rating}
                </span>
              </div>
            ) : null}
          </div>

          <div className="flex items-center gap-1 mt-1 text-gray-500 text-xs w-full">
            <MapPin size={12} className="shrink-0" />
            <span className="truncate flex-1 min-w-0" title={location}>
              {location}
            </span>
          </div>
        </div>

        {/* Category moved to bottom */}
        <div className="mt-auto pt-2 border-t border-slate-100/50 flex items-center gap-1.5 text-xs text-slate-500 w-full">
          <CategoryIcon name={category} iconUrl={categoryIcon} size={14} />

          <span
            className="font-medium bg-gray-100 px-2 py-0.5 rounded-full shrink truncate"
            title={category}
          >
            {category}
          </span>
          <span className="text-gray-400 ml-auto shrink-0 text-[10px] pl-1">
            ({reviews} đánh giá)
          </span>
        </div>
      </div>
    </motion.div>
  );
};

export default React.memo(LocationCard);
