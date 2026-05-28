import React from "react";
import { Search, Filter } from "lucide-react";
import { motion } from "framer-motion";

const TripFilters = ({
  currentFilter,
  onFilterChange,
  searchQuery,
  onSearchChange,
}) => {
  const filters = [
    { id: "all", label: "Tất cả" },
    { id: "ongoing", label: "Đang diễn ra" },
    { id: "planning", label: "Đang lên kế hoạch" },
    { id: "completed", label: "Đã hoàn thành" },
  ];

  return (
    <motion.div
      initial={{ opacity: 0, y: -10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.1 }}
      className="flex flex-col md:flex-row items-center justify-between gap-6 mb-12"
    >
      {/* Search */}
      <div className="relative w-full md:w-96 group">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 group-focus-within:text-blue-500 transition-colors" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Tìm kiếm chuyến đi..."
          className="w-full pl-11 pr-5 py-3 bg-white border border-gray-100 rounded-full shadow-sm text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all hover:bg-gray-50/50"
        />
      </div>

      {/* Filter Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto w-full md:w-auto pb-2 md:pb-0 hide-scrollbar p-1">
        <div className="bg-gray-100/50 p-1 rounded-full flex gap-1 border border-gray-100 w-max">
          {filters.map((filter) => (
            <button
              key={filter.id}
              onClick={() => onFilterChange(filter.id)}
              className={`relative px-4 py-2 rounded-full text-sm font-semibold whitespace-nowrap transition-colors duration-200 z-10 ${
                currentFilter === filter.id
                  ? "text-blue-600"
                  : "text-gray-500 hover:text-gray-700"
              }`}
            >
              {currentFilter === filter.id && (
                <motion.div
                  layoutId="activeFilter"
                  className="absolute inset-0 bg-white shadow-sm rounded-full"
                  transition={{ type: "spring", bounce: 0.2, duration: 0.6 }}
                />
              )}
              <span className="relative z-10">{filter.label}</span>
            </button>
          ))}
        </div>

        {/* Helper filter button */}
        <button className="hidden md:flex p-3 bg-white border border-gray-100 rounded-full hover:bg-gray-50 text-gray-400 hover:text-gray-600 shadow-sm transition-colors ml-2">
          <Filter className="w-4 h-4" />
        </button>
      </div>
    </motion.div>
  );
};

export default TripFilters;
