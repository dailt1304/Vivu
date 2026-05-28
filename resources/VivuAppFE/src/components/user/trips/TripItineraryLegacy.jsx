import React from "react";
import {
  Calendar,
  Clock,
  MapPin,
  Car,
  Footprints,
  Bus,
  Train,
  Plane,
} from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

const transportIcons = {
  taxi: Car,
  car: Car,
  walking: Footprints,
  bus: Bus,
  train: Train,
  flight: Plane,
  default: Car,
};

const TripItinerary = ({
  tripData,
  onLocationClick,
  onLocationHover,
  onLocationLeave,
}) => {
  if (!tripData?.tripDays?.length) {
    return (
      <div className="h-full flex items-center justify-center text-slate-400">
        <p>Chưa có lịch trình</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto bg-slate-50">
      <div className="max-w-2xl mx-auto p-6 space-y-6">
        {/* Header */}
        <div className="text-center mb-8">
          <h2 className="text-2xl font-bold text-slate-800">
            {tripData.title}
          </h2>
          {tripData.description && (
            <p className="text-slate-500 mt-2 text-sm">
              {tripData.description}
            </p>
          )}
          <div className="flex items-center justify-center gap-4 mt-3 text-xs text-slate-400">
            <span className="flex items-center gap-1">
              <Calendar size={12} />
              {tripData.startDate &&
                format(parseISO(tripData.startDate), "dd/MM/yyyy")}
              {" - "}
              {tripData.endDate &&
                format(parseISO(tripData.endDate), "dd/MM/yyyy")}
            </span>
          </div>
        </div>

        {/* Days */}
        {tripData.tripDays.map((day, dayIdx) => (
          <div
            key={dayIdx}
            className="bg-white rounded-xl shadow-sm border border-slate-100 overflow-hidden"
          >
            {/* Day Header */}
            <div className="bg-linear-to-r from-blue-600 to-cyan-500 px-5 py-3 text-white">
              <div className="flex items-center justify-between">
                <h3 className="font-bold text-lg">
                  {day.title || `Ngày ${day.dayIndex || dayIdx + 1}`}
                </h3>
                <span className="text-xs bg-white/20 px-2 py-1 rounded-full">
                  {day.dayDate &&
                    format(parseISO(day.dayDate), "EEEE, dd/MM", {
                      locale: vi,
                    })}
                </span>
              </div>
            </div>

            {/* Locations Timeline */}
            <div className="p-4">
              {day.locations?.map((loc, locIdx) => {
                const TransportIcon =
                  transportIcons[loc.transportMode] || transportIcons.default;
                const isLast = locIdx === day.locations.length - 1;
                const locationName =
                  loc.location?.name || `Địa điểm ${locIdx + 1}`;
                const locationDesc =
                  loc.note || loc.location?.description || "";

                return (
                  <div key={locIdx} className="relative flex gap-4">
                    {/* Timeline Line */}
                    <div className="flex flex-col items-center">
                      <div className="w-10 h-10 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-sm shrink-0 z-10">
                        {loc.orderIndex || locIdx + 1}
                      </div>
                      {!isLast && (
                        <div className="w-0.5 flex-1 bg-slate-200 my-1"></div>
                      )}
                    </div>

                    {/* Content - Clickable */}
                    <div
                      className={`flex-1 pb-6 cursor-pointer hover:bg-blue-50/50 rounded-lg p-2 -m-2 transition-all hover:scale-[1.01]`}
                      onClick={() => onLocationClick && onLocationClick(loc)}
                      onMouseEnter={() =>
                        onLocationHover && onLocationHover(loc)
                      }
                      onMouseLeave={() => onLocationLeave && onLocationLeave()}
                    >
                      {/* Time */}
                      <div className="flex items-center gap-2 text-xs text-slate-400 mb-1">
                        <Clock size={12} />
                        <span>
                          {loc.startTime?.slice(0, 5) || "--:--"}
                          {loc.endTime && ` - ${loc.endTime.slice(0, 5)}`}
                        </span>
                      </div>

                      {/* Location Name */}
                      <h4 className="font-semibold text-slate-800 flex items-center gap-2">
                        <MapPin size={14} className="text-blue-500" />
                        {locationName}
                      </h4>

                      {/* Description */}
                      {locationDesc && (
                        <p className="text-sm text-slate-500 mt-1 ml-5 leading-relaxed">
                          {locationDesc}
                        </p>
                      )}

                      {/* Transport to next */}
                      {!isLast && loc.transportMode && (
                        <div className="flex items-center gap-1.5 mt-3 ml-5 text-xs text-slate-400">
                          <TransportIcon size={12} />
                          <span className="capitalize">
                            {loc.transportMode}
                          </span>
                          <span>→ tiếp theo</span>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default TripItinerary;
