import React from "react";
import { Plus, MapPin, Star } from "lucide-react";
import { motion } from "framer-motion";

const MasonryLocationCard = ({ data }) => {
  const { title, location, image, rating } = data;

  return (
    <motion.div
      whileHover={{ y: -4 }}
      className="group relative break-inside-avoid bg-white rounded-3xl overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 ring-1 ring-slate-100 cursor-pointer"
    >
      <div className="relative">
        <img
          src={image}
          alt={title}
          className="w-full object-cover transition-transform duration-700 group-hover:scale-105"
        />

        {/* Glassmorphism gradient layer on hover */}
        <div className="absolute inset-0 bg-linear-to-t from-slate-900/80 via-slate-900/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

        {/* Floating Actions on Hover */}
        <div className="absolute inset-x-0 bottom-0 p-4 flex items-end justify-between opacity-0 group-hover:opacity-100 transition-all duration-300 translate-y-4 group-hover:translate-y-0">
          <p className="text-white font-bold leading-tight line-clamp-2 drop-shadow-md pr-2 w-full">
            {title}
          </p>
          <div className="flex gap-2 shrink-0">
            {data.actionType === 'remove' ? (
              <button 
                onClick={(e) => { e.stopPropagation(); data.onAction && data.onAction(); }}
                className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-red-500 flex items-center justify-center text-white shadow-lg transition-transform hover:scale-110"
              >
                <span className="text-xl leading-none -mt-1">&times;</span>
              </button>
            ) : (
              <button 
                onClick={(e) => { e.stopPropagation(); data.onAction && data.onAction(); }}
                className="w-9 h-9 sm:w-10 sm:h-10 rounded-full bg-gradient-primary flex items-center justify-center text-white shadow-lg transition-transform hover:scale-110"
              >
                <Plus size={20} className="stroke-3" />
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Info below image */}
      <div className="p-4">
        <h3 className="font-bold text-slate-800 text-base leading-tight group-hover:text-blue-600 transition-colors line-clamp-1 mb-1">
          {title}
        </h3>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1 text-slate-500 text-sm font-medium">
            <MapPin size={14} className="text-blue-500" />
            <span className="truncate max-w-[120px] sm:max-w-[160px]">
              {location}
            </span>
          </div>
          {rating && (
            <div className="flex items-center gap-1 font-bold text-slate-700 text-sm">
              <Star size={14} className="text-amber-400" fill="currentColor" />
              <span>{rating}</span>
            </div>
          )}
        </div>
      </div>
    </motion.div>
  );
};

export default MasonryLocationCard;
