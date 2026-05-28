import React, { memo } from "react";
import { MapPin, ImageIcon } from "lucide-react";

/**
 * Editorial Location Experience Card
 * List Layout: Image on left (square/rounded), robust content on the right.
 * Vercel Rule: rerender-memo
 */
const LocationExperienceCard = memo(
  ({
    destinationName,
    locationId,
    content,
    images = [],
    imageUrl,
    address,
    categoryName,
    categoryIconUrl,
    index = 0,
    onViewMap,
  }) => {
    const displayImages = images?.length > 0 ? images : imageUrl ? [imageUrl] : [];

    return (
      <div className="py-8 border-b border-gray-200/60 last:border-b-0 w-full group">
        <div className="flex flex-col md:flex-row gap-6 md:gap-8 w-full items-start">
          {/* Image Section - Scaled perfectly with massive rounded corners */}
          {displayImages.length > 0 && (
            <div className="w-full md:w-56 lg:w-64 aspect-[4/3] md:aspect-square relative rounded-3xl overflow-hidden bg-slate-100 shrink-0 border border-slate-100 group/slider">
              <div className="flex w-full h-full overflow-x-auto snap-x snap-mandatory scroll-smooth no-scrollbar" style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}>
                {displayImages.map((img, idx) => (
                  <div key={idx} className="w-full h-full shrink-0 snap-center relative">
                    <img
                      src={img}
                      alt={`${destinationName || "Location"} ${idx + 1}`}
                      className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 ease-out group-hover/slider:scale-105"
                      loading="lazy"
                    />
                  </div>
                ))}
              </div>
              
              {/* Photo Count Indicator */}
              {displayImages.length > 1 && (
                <div className="absolute top-3 right-3 flex items-center gap-1.5 bg-black/40 backdrop-blur-md px-2.5 py-1 rounded-full text-white text-[11px] font-semibold tracking-wide border border-white/10 z-10 pointer-events-none">
                  <ImageIcon size={12} className="opacity-90" />
                  <span>{displayImages.length}</span>
                </div>
              )}
            </div>
          )}

          {/* Content Section */}
          <div className="flex-1 w-full min-w-0 flex flex-col pt-1">
            <h4 className="text-xl md:text-[22px] font-bold text-gray-900 leading-snug mb-1 md:mb-1.5 tracking-tight break-words">
              {destinationName || "Địa điểm không xác định"}
            </h4>

            {/* Helper Meta Data (Address / Category) */}
            <div className="flex items-center text-[15px] font-medium text-gray-500 mb-4 truncate">
              {categoryName ? (
                <span className="flex items-center gap-1.5 flex-1 min-w-0 truncate">
                  {categoryIconUrl ? (
                    categoryIconUrl.startsWith("http") || categoryIconUrl.startsWith("/") ? (
                      <img src={categoryIconUrl} alt={categoryName} className="w-4 h-4 object-contain shrink-0" />
                    ) : (
                      <span className="shrink-0 leading-none">{categoryIconUrl}</span>
                    )
                  ) : (
                    <span className="shrink-0">🏢</span>
                  )}
                  <span className="truncate">{categoryName}</span>
                </span>
              ) : (
                <span className="flex items-center gap-1.5 flex-1 min-w-0 truncate">
                  <MapPin size={15} className="text-slate-400 shrink-0" />
                  <span className="truncate">{address || "Điểm đến trải nghiệm"}</span>
                </span>
              )}
            </div>

            {/* Blog Narrative Content */}
            <div className="prose prose-stone prose-p:leading-[1.7] prose-p:text-gray-600 prose-a:text-blue-600 hover:prose-a:underline max-w-none text-[15px] md:text-base break-words mb-6">
              {content ? (
                <div dangerouslySetInnerHTML={{ __html: content }} />
              ) : (
                <p className="text-slate-400 italic m-0">
                  Không có chia sẻ chi tiết nào cho địa điểm này.
                </p>
              )}
            </div>

            {/* Bottom Actions */}
            <div className="mt-auto">
              <button
                onClick={() => onViewMap && onViewMap(locationId)}
                disabled={!onViewMap}
                className="inline-flex items-center gap-2 px-5 py-2 rounded-full bg-white text-gray-800 text-[14px] font-semibold transition-all border border-gray-300 shadow-sm hover:shadow-md hover:bg-gray-50 hover:text-blue-600 hover:border-gray-300 focus:outline-none focus:ring-4 focus:ring-blue-50 cursor-pointer"
              >
                Xem chi tiết
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  },
);

LocationExperienceCard.displayName = "LocationExperienceCard";

export default LocationExperienceCard;
