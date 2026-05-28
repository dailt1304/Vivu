import React, { useRef, useEffect } from "react";
import { parse } from "date-fns";
import { Clock } from "lucide-react";

const START_HOUR = 0;
const END_HOUR = 24;
const HOURS_COUNT = END_HOUR - START_HOUR; // Removed +1 because 24 is exclusive upper bound logic typically, but let's check loop. Actually logical hours 0..23 needs 24 slots. 24:00 is end of day.
// If typical day is 0:00 to 23:59, we need slots 0 to 23.
// If END_HOUR=24, HOURS_COUNT = 24. Loop 0..23. Correct.

const PIXELS_PER_HOUR = 60; // Height of one hour slot

const CATEGORY_STYLES = {
  Attraction: {
    bg: "bg-blue-50",
    border: "border-blue-100",
    borderLeft: "border-blue-500",
    text: "text-blue-800",
    subText: "text-blue-600",
  },
  Shopping: {
    bg: "bg-purple-50",
    border: "border-purple-100",
    borderLeft: "border-purple-500",
    text: "text-purple-800",
    subText: "text-purple-600",
  },
  Nature: {
    bg: "bg-emerald-50",
    border: "border-emerald-100",
    borderLeft: "border-emerald-500",
    text: "text-emerald-800",
    subText: "text-emerald-600",
  },
  Historic: {
    bg: "bg-amber-50",
    border: "border-amber-100",
    borderLeft: "border-amber-500",
    text: "text-amber-800",
    subText: "text-amber-600",
  },
  Food: {
    bg: "bg-orange-50",
    border: "border-orange-100",
    borderLeft: "border-orange-500",
    text: "text-orange-800",
    subText: "text-orange-600",
  },
  Accommodation: {
    bg: "bg-indigo-50",
    border: "border-indigo-100",
    borderLeft: "border-indigo-500",
    text: "text-indigo-800",
    subText: "text-indigo-600",
  },
  Transport: {
    bg: "bg-slate-100",
    border: "border-slate-200",
    borderLeft: "border-slate-500",
    text: "text-slate-800",
    subText: "text-slate-600",
  },
  Entertainment: {
    bg: "bg-pink-50",
    border: "border-pink-100",
    borderLeft: "border-pink-500",
    text: "text-pink-800",
    subText: "text-pink-600",
  },
  default: {
    bg: "bg-gray-50",
    border: "border-gray-100",
    borderLeft: "border-gray-500",
    text: "text-gray-800",
    subText: "text-gray-600",
  },
};

const TripCalendar = ({ items, dayIds, getDateLabel }) => {
  const containerRef = useRef(null);

  // Initial scroll to 8:00 AM
  useEffect(() => {
    if (containerRef.current) {
      const scrollY = (8 - START_HOUR) * PIXELS_PER_HOUR;
      containerRef.current.scrollTop = scrollY;
    }
  }, []);

  const timeSlots = Array.from(
    { length: HOURS_COUNT },
    (_, i) => START_HOUR + i,
  );

  const getEventStyle = (startTime, endTime) => {
    if (!startTime || !endTime) return null;

    const start = parse(startTime, "HH:mm", new Date());
    const end = parse(endTime, "HH:mm", new Date());

    // Calculate minutes from start of day (0:00)
    // We set a base date at 0:00
    const baseDate = new Date();
    baseDate.setHours(START_HOUR, 0, 0, 0);

    // We need to handle the parsed dates correctly relative to base
    // Since parse('HH:mm') returns date=today, we just care about hours/minutes
    const startMinutes = start.getHours() * 60 + start.getMinutes();
    const endMinutes = end.getHours() * 60 + end.getMinutes();
    const baseMinutes = START_HOUR * 60;

    // Handle cross-day events
    let adjustedEndMinutes = endMinutes;
    if (adjustedEndMinutes < startMinutes) {
      adjustedEndMinutes += 24 * 60; // Add 24 hours
    }

    const top = ((startMinutes - baseMinutes) / 60) * PIXELS_PER_HOUR;
    const duration = adjustedEndMinutes - startMinutes;
    const height = (duration / 60) * PIXELS_PER_HOUR;

    return {
      top: `${Math.max(0, top)}px`,
      height: `${Math.max(30, height)}px`, // Minimum height 30px visibility
    };
  };

  return (
    <div className="flex flex-col h-full bg-white select-none">
      {/* Header: Days */}
      <div className="flex border-b border-gray-200 bg-gray-50 sticky top-0 z-30 shadow-xs">
        {/* Time Column Header (Spacer) */}
        <div className="w-16 shrink-0 border-r border-gray-200 bg-white"></div>

        {/* Days Headers */}
        <div className="flex flex-1 overflow-hidden">
          {dayIds.map((dayId, index) => (
            <div
              key={dayId}
              className="flex-1 min-w-[150px] border-r border-gray-100 p-3 text-center"
            >
              <div className="text-xs text-blue-600 font-bold uppercase tracking-wider mb-1">
                Ngày {index + 1}
              </div>
              <div className="text-sm font-bold text-gray-800">
                {getDateLabel(index)}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Scrollable Grid Area */}
      <div
        ref={containerRef}
        className="flex-1 overflow-y-auto relative custom-scrollbar pt-4"
      >
        <div className="flex relative min-h-full">
          {/* Time Labels Column */}
          <div className="w-16 shrink-0 border-r border-gray-200 bg-white sticky left-0 z-20">
            {timeSlots.map((hour) => (
              <div
                key={hour}
                className="border-b border-gray-50 relative box-border"
                style={{ height: PIXELS_PER_HOUR }}
              >
                <span className="absolute -top-2.5 right-2 text-xs font-medium text-gray-400 bg-white px-1">
                  {hour}:00
                </span>
              </div>
            ))}
          </div>

          {/* Grid Columns */}
          <div className="flex flex-1 relative">
            {/* Horizontal Grid lines (Background) */}
            <div className="absolute inset-0 z-0 pointer-events-none">
              {timeSlots.map((hour) => (
                <div
                  key={`line-${hour}`}
                  className="border-b border-gray-100 box-border"
                  style={{ height: PIXELS_PER_HOUR }}
                />
              ))}
            </div>

            {/* Day Columns */}
            {dayIds.map((dayId) => (
              <div
                key={dayId}
                className="flex-1 min-w-[150px] border-r border-gray-100 relative group"
              >
                {/* Hover effect for column */}
                <div className="absolute inset-0 bg-gray-50 opacity-0 group-hover:opacity-40 transition-opacity pointer-events-none" />

                {/* Events */}
                {items[dayId]?.map((item) => {
                  const style = getEventStyle(item.startTime, item.endTime);
                  if (!style) return null;

                  const catName =
                    typeof item.category === "object"
                      ? item.category?.name
                      : item.category;
                  const colors =
                    CATEGORY_STYLES[catName] || CATEGORY_STYLES.default;

                  return (
                    <div
                      key={item.id}
                      className={`absolute left-1 right-1 rounded-lg border p-2 shadow-sm hover:shadow-md hover:scale-[1.02] hover:z-10 transition-all cursor-pointer overflow-hidden ${colors.bg} ${colors.border}`}
                      style={style}
                      title={`${item.content} (${item.startTime} - ${item.endTime})`}
                    >
                      <div
                        className={`h-full border-l-4 pl-2 ${colors.borderLeft}`}
                      >
                        <div
                          className={`text-xs font-bold mb-0.5 truncate leading-tight ${colors.text}`}
                        >
                          {item.content}
                        </div>
                        <div
                          className={`flex items-center gap-1 text-[10px] font-medium opacity-80 ${colors.subText}`}
                        >
                          <Clock size={10} />
                          <span>
                            {item.startTime} - {item.endTime}
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default TripCalendar;
