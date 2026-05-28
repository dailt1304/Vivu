import React, { useState, useEffect, useMemo, useCallback } from "react";
import { ChevronDown, Calendar, Map as MapIcon } from "lucide-react";
import { format, parseISO } from "date-fns";

import MapContainer from "../../common/map/MapContainer";
import { useRouteDirection } from "../../../hooks/maps/useRouteDirection";
import { useCityById } from "../../../hooks/cities/useCities";

const TripMapPanel = ({ tripData, hoveredItemId }) => {
  const [selectedMapDayIndex, setSelectedMapDayIndex] = useState("ALL");
  const [isMapDayDropdownOpen, setIsMapDayDropdownOpen] = useState(false);
  const [flyToLocation, setFlyToLocation] = useState(null);

  const filteredTripDays = useMemo(() => {
    if (selectedMapDayIndex === "ALL") {
      return tripData?.tripDays?.filter((d) => d.dayIndex !== 0) || [];
    }
    return (
      tripData?.tripDays?.filter((d) => d.dayIndex === selectedMapDayIndex) ||
      []
    );
  }, [tripData?.tripDays, selectedMapDayIndex]);

  const scheduledItemIds = useMemo(
    () =>
      new Set(
        filteredTripDays.flatMap((day) =>
          day.locations?.map((loc) => loc.locationId),
        ) || [],
      ),
    [filteredTripDays],
  );

  const allMapLocations = useMemo(() => {
    const scheduled =
      filteredTripDays.flatMap((day) =>
        day.locations?.map((loc) => ({
          id: loc.locationId,
          title: loc.location?.name || "Địa điểm",
          category: loc.location?.category?.name || "Default",
          categoryIcon: loc.location?.category?.iconUrl,
          orderIndex: loc.orderIndex,
          latitude: loc.location?.latitude,
          longitude: loc.location?.longitude,
        })),
      ) || [];

    const ideas =
      tripData?.ideas?.map((idea) => ({
        id: idea.locationId,
        title: idea.location?.name || "Ý tưởng",
        category: idea.location?.category?.name || "Default",
        categoryIcon: idea.location?.category?.iconUrl,
        latitude: idea.location?.latitude,
        longitude: idea.location?.longitude,
      })) || [];

    return [...ideas, ...scheduled];
  }, [filteredTripDays, tripData?.ideas]);

  const scheduledMapLocations = useMemo(
    () =>
      filteredTripDays.flatMap((day) =>
        day.locations?.map((loc) => ({
          id: loc.locationId,
          title: loc.location?.name || "Địa điểm",
          category: loc.location?.category?.name || "Default",
          categoryIcon: loc.location?.category?.iconUrl,
          orderIndex: loc.orderIndex,
          latitude: loc.location?.latitude,
          longitude: loc.location?.longitude,
        })),
      ) || [],
    [filteredTripDays],
  );

  const { routeData } = useRouteDirection(scheduledMapLocations);

  const handleMapMarkerClick = useCallback((loc) => {
    setFlyToLocation({
      latitude: loc.latitude,
      longitude: loc.longitude,
      zoom: 16,
    });
  }, []);

  const { data: cityData } = useCityById(tripData?.cityId);

  // Auto-zoom to city overview if no locations exist
  useEffect(() => {
    if (
      allMapLocations.length === 0 &&
      cityData?.latitude &&
      cityData?.longitude
    ) {
      setFlyToLocation({
        latitude: cityData.latitude,
        longitude: cityData.longitude,
        zoom: 12,
      });
    }
  }, [cityData, allMapLocations.length]);

  return (
    <div className="hidden md:block w-1/2 h-full relative z-0">
      <div className="absolute inset-0">
        <MapContainer
          locations={allMapLocations}
          hoveredItemId={hoveredItemId}
          routeData={selectedMapDayIndex === "ALL" ? null : routeData}
          scheduledItemIds={scheduledItemIds}
          flyToLocation={flyToLocation}
          onMarkerClick={handleMapMarkerClick}
          hideMarkerNumbers={selectedMapDayIndex === "ALL"}
        />

        {/* Custom Map Day Selector Overlay */}
        <div className="absolute top-4 right-14 z-10001 w-48">
          <div className="relative">
            <button
              type="button"
              onClick={() => setIsMapDayDropdownOpen(!isMapDayDropdownOpen)}
              className="w-full flex items-center justify-between bg-white/90 backdrop-blur-md rounded-xl shadow-lg border border-gray-100/50 px-4 py-2.5 hover:bg-white hover:shadow-xl transition-all duration-200 group ring-1 ring-black/5"
            >
              <div className="flex items-center gap-2">
                <Calendar size={16} className="text-blue-500" />
                <span className="text-sm font-bold text-gray-700">
                  {selectedMapDayIndex === "ALL"
                    ? "Tất cả các ngày"
                    : `Ngày ${selectedMapDayIndex}`}
                </span>
              </div>
              <ChevronDown
                size={16}
                className={`text-gray-400 transition-transform duration-300 ${isMapDayDropdownOpen ? "rotate-180 text-blue-500" : "group-hover:text-gray-600"}`}
              />
            </button>

            {/* Dropdown Menu */}
            <div
              className={`absolute top-full right-0 mt-2 w-56 bg-white rounded-xl shadow-2xl border border-gray-100 overflow-hidden transition-all duration-300 origin-top transform ${
                isMapDayDropdownOpen
                  ? "opacity-100 scale-100 translate-y-0"
                  : "opacity-0 scale-95 -translate-y-2 pointer-events-none"
              }`}
            >
              <div className="p-2 space-y-1 max-h-[300px] overflow-y-auto">
                <button
                  type="button"
                  onClick={() => {
                    setSelectedMapDayIndex("ALL");
                    setIsMapDayDropdownOpen(false);
                  }}
                  className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                    selectedMapDayIndex === "ALL"
                      ? "bg-blue-50 text-blue-700"
                      : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <MapIcon size={16} className={selectedMapDayIndex === "ALL" ? "text-blue-500" : "text-gray-400"} />
                    <span>Tất cả các ngày</span>
                  </div>
                </button>

                {tripData?.tripDays?.filter((d) => d.dayIndex !== 0).map((day) => (
                  <button
                    key={day.tripDayId || day.id}
                    type="button"
                    onClick={() => {
                      setSelectedMapDayIndex(day.dayIndex);
                      setIsMapDayDropdownOpen(false);
                    }}
                    className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                      selectedMapDayIndex === day.dayIndex
                        ? "bg-blue-50 text-blue-700"
                        : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                    }`}
                  >
                    <div className="flex flex-col items-start gap-0.5">
                      <span className="font-semibold text-gray-800">Ngày {day.dayIndex}</span>
                      {day.date && (
                        <span className="text-[10px] text-gray-500">
                          {format(parseISO(tripData.startDate), "dd/MM")} (Dự kiến)
                        </span>
                      )}
                    </div>
                    {selectedMapDayIndex === day.dayIndex && (
                      <div className="w-1.5 h-1.5 rounded-full bg-blue-500" />
                    )}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default React.memo(TripMapPanel);
