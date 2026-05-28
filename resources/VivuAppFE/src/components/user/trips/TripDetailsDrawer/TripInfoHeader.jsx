import { MapPin, Calendar, Users, Pencil, Check } from "lucide-react";
import { format } from "date-fns";
import React, { useState } from "react";

/**
 * TripInfoHeader - Trip title, location, date range, and trip size
 *
 * Extracted from TripDetailsDrawer to reduce component complexity
 * and enable independent testing/styling.
 */
function TripInfoHeader({
  tripTitle,
  onTitleChange,
  onTitleSave,
  startDate,
  endDate,
  onDateClick,
  tripSize,
  onTripSizeChange,
  onTripSizeSave,
  destination,
}) {
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [isEditingTripSize, setIsEditingTripSize] = useState(false);
  const [localTripSize, setLocalTripSize] = useState("");

  // Format date range
  const formattedDateRange = `${format(startDate, "dd/MM")} - ${format(endDate, "dd/MM")}`;

  const handleTitleSave = () => {
    setIsEditingTitle(false);
    if (onTitleSave) onTitleSave();
  };

  const handleTripSizeSave = () => {
    setIsEditingTripSize(false);
    const numVal = parseInt(localTripSize);
    if (localTripSize && numVal >= 1) {
      onTripSizeChange(numVal);
      if (onTripSizeSave) onTripSizeSave();
    } else {
      // Reset to original if invalid
      setLocalTripSize(String(tripSize || 1));
    }
  };

  return (
    <div className="px-6 py-6 bg-white">
      <div className="flex flex-col gap-1">
        {/* Editable Title */}
        {isEditingTitle ? (
          <div className="flex items-center gap-2">
            <input
              autoFocus
              type="text"
              value={tripTitle}
              onChange={(e) => onTitleChange(e.target.value)}
              onBlur={handleTitleSave}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleTitleSave();
              }}
              className="font-bold text-3xl text-slate-800 leading-tight bg-white border-2 border-blue-500 rounded-lg px-2 py-1 outline-none w-full shadow-sm"
            />
            <button
              onMouseDown={(e) => e.preventDefault()}
              onClick={handleTitleSave}
              className="p-1.5 text-white bg-linear-to-r from-blue-500 to-cyan-500 rounded-full hover:shadow-lg transition-all"
            >
              <Check size={20} />
            </button>
          </div>
        ) : (
          <div
            className="group flex items-center gap-3 cursor-pointer -ml-2 p-2 hover:bg-slate-50 rounded-xl transition-colors w-fit"
            onClick={() => setIsEditingTitle(true)}
          >
            <h2 className="font-bold text-3xl text-slate-800 leading-tight group-hover:text-blue-600 transition-colors">
              {tripTitle}
            </h2>
            <div className="opacity-0 group-hover:opacity-100 transition-all duration-200 p-1.5 bg-white shadow-sm border border-slate-100 text-slate-400 rounded-full hover:border-blue-200 hover:text-blue-600">
              <Pencil size={14} />
            </div>
          </div>
        )}

        {/* Metadata Row */}
        <div className="flex items-center gap-4 text-sm text-slate-500 mt-2 font-medium">
          {/* Destination */}
          <span className="flex items-center gap-1.5 bg-slate-50 px-2 py-1 rounded-md">
            <MapPin size={14} className="text-slate-400" />
            {destination}
          </span>

          {/* Date Range */}
          <span
            className="flex items-center gap-1.5 cursor-pointer hover:text-blue-600 hover:bg-blue-50 px-2 py-1 rounded-md transition-colors"
            onClick={onDateClick}
            title="Thay đổi thời gian"
          >
            <Calendar size={14} className="text-slate-400" />
            {formattedDateRange}
          </span>

          {/* Trip Size */}
          {isEditingTripSize ? (
            <div className="flex items-center gap-1">
              <input
                autoFocus
                type="text"
                inputMode="numeric"
                value={localTripSize}
                onChange={(e) => {
                  const val = e.target.value;
                  if (val === "" || /^[0-9]+$/.test(val)) {
                    setLocalTripSize(val);
                  }
                }}
                onBlur={handleTripSizeSave}
                onKeyDown={(e) => {
                  if (e.key === "Enter") e.target.blur();
                }}
                className="w-12 text-center bg-white border border-blue-500 rounded px-1 py-0.5 text-sm font-medium"
              />
              <span className="text-slate-500">người</span>
            </div>
          ) : (
            <span
              className="flex items-center gap-1.5 cursor-pointer hover:text-blue-600 hover:bg-blue-50 px-2 py-1 rounded-md transition-colors"
              onClick={() => {
                setLocalTripSize(String(tripSize || 1));
                setIsEditingTripSize(true);
              }}
              title="Thay đổi số người"
            >
              <Users size={14} className="text-slate-400" />
              {tripSize || 1} người
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

export default React.memo(TripInfoHeader);
