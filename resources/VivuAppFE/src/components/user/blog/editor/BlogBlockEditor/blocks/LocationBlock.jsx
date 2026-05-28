import React, { memo } from "react";
import { MapPin, ImageIcon, Star, Clock, LayoutGrid } from "lucide-react";
import MiniEditor from "../MiniEditor";

/**
 * LocationBlock
 * Displays location info from the trip dynamically and allows writing an "Experience" story.
 * Vercel Rule: rerender-memo
 */
const LocationBlock = memo(
  ({
    destinationName,
    content,
    images = [],
    imageUrl,
    rating,
    categoryName,
    address,
    timeRange,
    onUpdate,
  }) => {
    const displayImages = images?.length > 0 ? images : imageUrl ? [imageUrl] : [];

    return (
      <div className="flex flex-col md:flex-row mb-12 bg-white rounded-[2rem] md:rounded-[2.5rem] border border-slate-100 shadow-[0_8px_30px_rgb(0,0,0,0.04)] overflow-hidden transition-all duration-300 hover:shadow-[0_8px_40px_rgb(0,0,0,0.08)]">
        {/* Cover Image Section - Slider */}
        <div className="relative w-full md:w-5/12 aspect-[4/3] md:aspect-auto bg-slate-100 shrink-0 overflow-hidden group/img">
          {displayImages.length > 0 ? (
            <div className="flex w-full h-full overflow-x-auto snap-x snap-mandatory scroll-smooth no-scrollbar" style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}>
              {displayImages.map((img, idx) => (
                <div key={idx} className="w-full h-full shrink-0 snap-center relative">
                  <img
                    src={img}
                    alt={`${destinationName} ${idx + 1}`}
                    className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover/img:scale-105"
                  />
                  {/* Overlay gradient for readability */}
                  <div className="absolute inset-x-0 bottom-0 h-1/2 bg-gradient-to-t from-black/30 to-transparent pointer-events-none" />
                </div>
              ))}
            </div>
          ) : (
            <div className="w-full h-full flex flex-col items-center justify-center text-slate-300 bg-gradient-to-br from-slate-50 to-slate-200">
              <ImageIcon size={48} strokeWidth={1.5} />
            </div>
          )}
          
          {/* Photo Count Indicator */}
          {displayImages.length > 1 && (
            <div className="absolute top-4 right-4 flex items-center gap-1.5 bg-black/40 backdrop-blur-md px-3 py-1.5 rounded-full text-white text-xs font-semibold tracking-wide border border-white/10 z-10 pointer-events-none">
              <ImageIcon size={14} className="opacity-90" />
              <span>{displayImages.length}</span>
            </div>
          )}
        </div>

        {/* Content & Editor Section */}
        <div className="flex-1 flex flex-col p-6 md:p-8 min-w-0">
          
          {/* Title Area */}
          <div className="mb-4">
            <h4 className="font-extrabold text-2xl md:text-3xl text-slate-900 tracking-tight leading-tight line-clamp-2">
              {destinationName || "Địa điểm không tên"}
            </h4>
          </div>

          {/* Meta Info (Category, Rating, Time, Location) */}
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm font-medium text-slate-500 mb-6">
            {categoryName && (
              <span className="flex items-center gap-1.5 text-blue-600 bg-blue-50 px-2.5 py-1 rounded-lg">
                <LayoutGrid size={14} strokeWidth={2.5} />
                <span className="max-w-[120px] truncate">{categoryName}</span>
              </span>
            )}
            
            {rating > 0 && (
              <span className="flex items-center gap-1.5 text-amber-500 bg-amber-50 px-2.5 py-1 rounded-lg">
                <Star size={14} className="fill-current" />
                <span>{rating.toFixed(1)}</span>
              </span>
            )}
            
            {timeRange && (
              <span className="flex items-center gap-1.5 text-slate-600 bg-slate-50 px-2.5 py-1 rounded-lg">
                <Clock size={14} className="text-slate-400" />
                <span className="truncate">{timeRange}</span>
              </span>
            )}
            
            {address && (
              <span className="flex items-center gap-1.5 text-slate-600 bg-slate-50 px-2.5 py-1 rounded-lg w-full mt-1">
                <MapPin size={14} className="text-slate-400 shrink-0" />
                <span className="truncate">{address}</span>
              </span>
            )}
          </div>

          <div className="w-12 h-1 bg-blue-500/20 rounded-full mb-6"></div>

          {/* Narrative / Editor Canvas */}
          <div className="flex-1 rounded-2xl min-h-[120px] focus-within:ring-4 focus-within:ring-blue-500/10 transition-all border border-transparent focus-within:border-blue-200 hover:border-slate-200/50 bg-slate-50/50 p-4 -mx-4 group">
            <MiniEditor
              content={content}
              onChange={(html) => onUpdate({ content: html })}
              placeholder="Chia sẻ trải nghiệm của bạn tại đây..."
            />
          </div>
        </div>
      </div>
    );
  },
);

LocationBlock.displayName = "LocationBlock";

export default LocationBlock;
