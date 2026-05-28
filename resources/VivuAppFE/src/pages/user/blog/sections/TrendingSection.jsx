import React from "react";
import BlogCard from "../../../../components/common/cards/BlogCard";
import TripCard from "../../../../components/common/cards/TripCard";
import { Flame } from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, Pagination } from "swiper/modules";

// Import Swiper styles
import "swiper/css";
import "swiper/css/pagination";

/**
 * TrendingSection V3
 * Blogs -> Bento Grid Layout (1 large + 2 small)
 * Trips -> Auto-moving Carousel Slider
 */
const TrendingSkeleton = () => (
  <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
    <div className="md:col-span-2 h-[280px] sm:h-[360px] md:h-[480px] rounded-3xl bg-gray-200 animate-pulse" />
    <div className="hidden md:flex md:col-span-1 flex-col gap-6 h-[480px]">
      <div className="flex-1 rounded-3xl bg-gray-200 animate-pulse" />
      <div className="flex-1 rounded-3xl bg-gray-200 animate-pulse" />
    </div>
  </div>
);

export default function TrendingSection({
  type,
  items,
  loading,
  favoriteTripIds,
  bookmarkedBlogIds = new Set(),
}) {
  if (!loading && (!items || items.length < 3)) return null;

  return (
    <div className="mb-0">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl sm:text-2xl font-black text-gray-900 flex items-center gap-2 tracking-tight">
          <div className="p-1.5 sm:p-2 bg-gradient-primary rounded-lg sm:rounded-xl shadow-sm">
            <Flame className="w-4 h-4 sm:w-5 sm:h-5 text-white fill-white" />
          </div>
          Xu Hướng
        </h2>
      </div>

      {loading ? (
        <TrendingSkeleton />
      ) : type === "trips" ? (
        /* CAROUSEL SLIDER FOR TRIPS */
        <Swiper
          modules={[Autoplay, Pagination]}
          spaceBetween={24}
          slidesPerView={1}
          loop={true}
          speed={800}
          breakpoints={{
            640: { slidesPerView: 2 },
            1024: { slidesPerView: 3 },
          }}
          autoplay={{ delay: 3000, disableOnInteraction: true }}
          pagination={{ clickable: true, dynamicBullets: true }}
          className="pt-2! pb-14! [&_.swiper-wrapper]:ease-[ease-in-out]!" // Generous padding to prevent shadow/hover clipping
        >
          {items.map((trip) => (
            <SwiperSlide key={trip.id} className="h-auto">
              <div className="h-full">
                <TripCard
                  trip={trip}
                  isFavorited={favoriteTripIds?.has(trip.id?.toLowerCase())}
                />
              </div>
            </SwiperSlide>
          ))}
        </Swiper>
      ) : (
        /* REGULAR GRID FOR BLOGS (Minimalist) */
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
          {items.slice(0, 3).map((item) => (
            <BlogCard
              key={item.id}
              data={{
                ...item,
                isBookmarked: bookmarkedBlogIds.has(item.id?.toLowerCase()),
              }}
            />
          ))}
        </div>
      )}
    </div>
  );
}
