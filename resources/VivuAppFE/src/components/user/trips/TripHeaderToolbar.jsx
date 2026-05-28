import React from "react";
import { format } from "date-fns";
import { Briefcase } from "lucide-react";

const TripHeaderToolbar = ({
  tripData,
  cityData,
  startDate,
  endDate,
  id,
  isDrawerOpen,
  setIsDrawerOpen,
}) => {
  return (
    <div className="w-full border-b border-gray-100 bg-white/70 backdrop-blur-xl py-3 px-4 md:px-6 flex items-center justify-between z-40 shrink-0 sticky top-0">
      {/* Left: Title & Context */}
      <div className="flex items-center gap-3 md:gap-4 overflow-hidden">
        <div className="flex flex-col overflow-hidden max-w-[150px] md:max-w-xs">
          <h1 className="font-bold text-gray-900 text-sm md:text-base truncate tracking-tight">
            {tripData?.title || "Chuyến đi"}
          </h1>
          <p className="text-[10px] md:text-xs text-gray-500 font-medium tracking-wide truncate mt-0.5">
            {cityData?.name || "Thành phố"} • {tripData?.tripSize || 1} người
          </p>
        </div>
        {id !== "new" && (
          <>
            <div className="hidden md:block h-6 w-px bg-gray-200 mx-2"></div>
            <div className="hidden md:flex flex-col items-start ml-1">
              <span className="text-[10px] text-gray-400 font-semibold uppercase tracking-wider">
                Thời gian
              </span>
              <span className="text-xs text-gray-700 font-bold">
                {format(startDate, "dd/MM")} - {format(endDate, "dd/MM")}
              </span>
            </div>
          </>
        )}
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-2 shrink-0">
        <button
          onClick={() => setIsDrawerOpen(!isDrawerOpen)}
          className={`group relative flex items-center gap-1.5 md:gap-2 px-3 py-2 md:px-5 md:py-2.5 rounded-full shadow-sm transition-all duration-300 overflow-hidden border ${
            isDrawerOpen
              ? "bg-gradient-primary border-transparent text-white shadow-md shadow-blue-500/30"
              : "bg-white border-gray-200 text-gray-700 hover:border-gray-300 hover:shadow-md hover:bg-gray-50"
          }`}
        >
          {isDrawerOpen && (
            <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity"></div>
          )}
          <Briefcase
            size={14}
            className={`${isDrawerOpen ? "text-white" : "text-gray-500"} transition-colors`}
          />
          <span
            className={`text-[11px] md:text-xs font-bold tracking-wide ${isDrawerOpen ? "text-white" : "text-gray-800"}`}
          >
            Lịch trình
          </span>
          <span
            className={`flex items-center justify-center h-4 w-4 md:h-5 md:w-5 rounded-full text-[9px] font-bold transition-colors ${
              isDrawerOpen
                ? "bg-white/20 text-white"
                : "bg-gray-100 text-gray-600"
            }`}
          >
            {tripData?.tripDays?.length || 0}
          </span>
        </button>
      </div>
    </div>
  );
};

export default React.memo(TripHeaderToolbar);
