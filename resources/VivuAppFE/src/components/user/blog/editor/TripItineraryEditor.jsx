import React, { memo, useMemo, useCallback, useEffect } from "react";
import { MapPin, Calendar } from "lucide-react";
import useTripDetails from "../../../../hooks/trips/useTripDetails";

// Vercel Rule: rendering-hoist-jsx — static outside render
const MAX_CONTENT_LENGTH = 500;
const SECTION_TITLE = "Lịch trình chuyến đi";

/**
 * LocationContentItem — textarea for a single location
 * Vercel Rule: rerender-memo
 */
const LocationContentItem = memo(
  ({ locationName, displayOrder, content, onChange }) => {
    // Vercel Rule: rerender-move-effect-to-event
    const handleChange = useCallback(
      (e) => {
        const val = e.target.value;
        if (val.length <= MAX_CONTENT_LENGTH) {
          onChange(displayOrder, val);
        }
      },
      [displayOrder, onChange],
    );

    return (
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
        <div className="flex items-center gap-2 px-4 py-3 bg-gray-50/80 border-b border-gray-100">
          <MapPin size={14} className="text-blue-500 shrink-0" />
          <span className="text-sm font-bold text-gray-700 truncate">
            {locationName || `Địa điểm ${displayOrder + 1}`}
          </span>
        </div>
        <div className="p-4">
          <textarea
            value={content || ""}
            onChange={handleChange}
            placeholder={`Chia sẻ trải nghiệm tại ${locationName || "đây"}...`}
            rows={3}
            className="w-full resize-none border border-gray-200 rounded-lg px-3 py-2.5 text-sm text-gray-700 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500/30 focus:border-blue-400 transition-all"
          />
          <div className="flex justify-end mt-1">
            <span
              className={`text-xs ${
                (content?.length || 0) > MAX_CONTENT_LENGTH * 0.9
                  ? "text-amber-500"
                  : "text-gray-400"
              }`}
            >
              {content?.length || 0}/{MAX_CONTENT_LENGTH}
            </span>
          </div>
        </div>
      </div>
    );
  },
);

LocationContentItem.displayName = "LocationContentItem";

/**
 * TripItineraryEditor — Displays trip itinerary with per-location textareas.
 *
 * @param {string} tripId - The selected trip ID
 * @param {Array} storyDays - Current story day content array
 * @param {Function} onStoryDaysChange - Callback with updated storyDays array
 *
 * Vercel Rules: rendering-conditional-render, rerender-derived-state-no-effect,
 *   rendering-hoist-jsx, rerender-memo, js-early-exit
 */
const TripItineraryEditor = memo(({ tripId, storyDays, onStoryDaysChange }) => {
  const { trip, loading } = useTripDetails(tripId);

  // Vercel Rule: rerender-derived-state-no-effect — group locations by day
  const dayGroups = useMemo(() => {
    if (!trip?.tripDays) return [];

    return trip.tripDays
      .slice()
      .sort((a, b) => (a.dayIndex ?? 0) - (b.dayIndex ?? 0))
      .map((day) => ({
        dayIndex: day.dayIndex,
        title: day.title || `Ngày ${day.dayIndex}`,
        date: day.dayDate,
        locations: (day.locations || [])
          .slice()
          .sort((a, b) => (a.orderIndex ?? 0) - (b.orderIndex ?? 0))
          .map((loc) => ({
            name: loc.location?.name || loc.locationName || "Không rõ",
            locationId: loc.locationId,
          })),
      }));
  }, [trip]);

  // Vercel Rule: rerender-move-effect-to-event
  const handleContentChange = useCallback(
    (displayOrder, newContent) => {
      onStoryDaysChange?.((prev) =>
        prev.map((sd) =>
          sd.displayOrder === displayOrder
            ? { ...sd, content: newContent }
            : sd,
        ),
      );
    },
    [onStoryDaysChange],
  );

  // Initialize storyDays from trip data when trip first loads (create mode)
  useEffect(() => {
    if (!dayGroups.length || storyDays.length > 0) return;

    let displayOrder = 0;
    const initial = [];
    for (const day of dayGroups) {
      for (const loc of day.locations) {
        initial.push({
          dayNumber: day.dayIndex,
          title: day.title,
          content: "",
          destinationName: loc.name,
          displayOrder: displayOrder++,
        });
      }
    }
    if (initial.length > 0) {
      onStoryDaysChange?.(initial);
    }
  }, [dayGroups, storyDays.length, onStoryDaysChange]);

  // Vercel Rule: js-early-exit + rendering-conditional-render
  if (!tripId) return null;

  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <div className="w-8 h-8 rounded-full border-4 border-gray-200 border-t-blue-500 animate-spin" />
      </div>
    );
  }

  if (!dayGroups.length) {
    return (
      <div className="text-center py-8 text-gray-400 text-sm">
        Chuyến đi chưa có lịch trình nào.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
        <Calendar size={18} className="text-blue-500" />
        {SECTION_TITLE}
      </h3>

      {dayGroups.map((day) => (
        <div
          key={day.dayIndex}
          className="rounded-2xl border border-gray-100 bg-gray-50/50 overflow-hidden"
        >
          {/* Day Header */}
          <div className="px-5 py-3 bg-linear-to-r from-blue-50 to-indigo-50 border-b border-gray-100">
            <div className="flex items-center gap-2">
              <span className="text-sm font-black text-blue-600">
                📅 {day.title}
              </span>
              {day.date && (
                <span className="text-xs text-gray-400 font-medium">
                  —{" "}
                  {new Date(day.date).toLocaleDateString("vi-VN", {
                    day: "2-digit",
                    month: "2-digit",
                    year: "numeric",
                  })}
                </span>
              )}
            </div>
          </div>

          {/* Location Editors */}
          <div className="p-4 space-y-3">
            {day.locations.length > 0 ? (
              day.locations.map((loc, locIdx) => {
                // Find matching story day by dayNumber + destination
                const matchingSD = storyDays.find(
                  (sd) =>
                    sd.dayNumber === day.dayIndex &&
                    sd.destinationName === loc.name,
                );
                const displayOrder = matchingSD?.displayOrder ?? locIdx;

                return (
                  <LocationContentItem
                    key={`${day.dayIndex}-${loc.locationId || locIdx}`}
                    locationName={loc.name}
                    displayOrder={displayOrder}
                    content={matchingSD?.content || ""}
                    onChange={handleContentChange}
                  />
                );
              })
            ) : (
              <p className="text-sm text-gray-400 text-center py-4">
                Ngày này chưa có địa điểm nào.
              </p>
            )}
          </div>
        </div>
      ))}
    </div>
  );
});

TripItineraryEditor.displayName = "TripItineraryEditor";

export default TripItineraryEditor;
