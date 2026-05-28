import React from "react";
import TripCard from "../../../../components/common/cards/TripCard";
import { Map, Search } from "lucide-react";
import { motion } from "framer-motion";

const TripSkeletonGrid = () => (
  <div
    className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-6 relative"
  >
    {[...Array(10)].map((_, i) => (
      <div key={`skel-trip-${i}`} className="flex flex-col gap-2.5 w-full">
        <div className="rounded-2xl bg-gray-200 animate-pulse aspect-[4/3] sm:aspect-[4/5] w-full" />
        <div className="h-4 bg-gray-200 rounded-md w-3/4 animate-pulse mt-1" />
        <div className="h-3 bg-gray-200 rounded-md w-1/2 animate-pulse" />
      </div>
    ))}
  </div>
);

export default function TripFeedSection({
  trips,
  loading,
  currentPage,
  totalPages,
  onPageChange,
  searchQuery,
  onSearchChange,
  favoriteTripIds,
}) {
  return (
    <div className="mt-4">
      {/* Header Row: Title */}
      <h2 className="text-[22px] sm:text-2xl font-bold text-gray-900 mb-6 tracking-tight">
        Lịch trình cộng đồng
      </h2>

      {loading && trips.length === 0 ? (
        <TripSkeletonGrid />
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-6 mb-10 pt-2 min-h-[600px]">
          {trips.map((trip, idx) => (
            <motion.div
              key={trip.id}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: (idx % 9) * 0.05 }}
              className="flex h-full"
              style={{
                contentVisibility: "auto",
                containIntrinsicSize: "auto 380px",
              }}
            >
              <TripCard
                trip={trip}
                isFavorited={favoriteTripIds?.has(trip.id?.toLowerCase())}
              />
            </motion.div>
          ))}
        </div>
      )}

      {loading && trips.length > 0 && (
        <div className="flex justify-center my-6">
          <div className="w-8 h-8 rounded-full border-4 border-blue-200 border-t-blue-600 animate-spin" />
        </div>
      )}

      {!loading && trips.length === 0 && (
        <div className="py-20 text-center text-gray-500 font-medium">
          {searchQuery
            ? `Không tìm thấy lịch trình cho "${searchQuery}"`
            : "Chưa có chuyến đi nào được chia sẻ."}
        </div>
      )}

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex justify-center items-center gap-2 mt-12 mb-8">
          <button
            onClick={() => onPageChange(currentPage - 1)}
            disabled={currentPage === 1 || loading}
            className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors"
          >
            &lt;
          </button>

          {[...Array(totalPages)].map((_, i) => {
            const pageNum = i + 1;
            // Simple pagination logic, showing nearby pages
            if (
              pageNum === 1 ||
              pageNum === totalPages ||
              (pageNum >= currentPage - 1 && pageNum <= currentPage + 1)
            ) {
              return (
                <button
                  key={pageNum}
                  onClick={() => onPageChange(pageNum)}
                  className={`w-10 h-10 flex items-center justify-center rounded-xl text-sm font-bold transition-all ${
                    currentPage === pageNum
                      ? "bg-gradient-primary text-white shadow-md shadow-blue-500/20"
                      : "border border-gray-200 text-gray-600 hover:bg-gray-50 hover:text-blue-600"
                  }`}
                >
                  {pageNum}
                </button>
              );
            } else if (
              pageNum === currentPage - 2 ||
              pageNum === currentPage + 2
            ) {
              return (
                <span key={pageNum} className="text-gray-400">
                  ...
                </span>
              );
            }
            return null;
          })}

          <button
            onClick={() => onPageChange(currentPage + 1)}
            disabled={currentPage === totalPages || loading}
            className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors"
          >
            &gt;
          </button>
        </div>
      )}
    </div>
  );
}
