import React, { useState } from "react";
import {
  format,
  addMonths,
  subMonths,
  startOfMonth,
  endOfMonth,
  startOfWeek,
  endOfWeek,
  addDays,
  isSameMonth,
  isSameDay,
  isWithinInterval,
  isBefore,
} from "date-fns";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { vi } from "date-fns/locale";

const DateRangePicker = ({ startDate, endDate, onChange }) => {
  // Determine initial view: if startDate exists, center around it.
  const [currentMonth, setCurrentMonth] = useState(
    startDate ? startOfMonth(startDate) : startOfMonth(new Date()),
  );
  const [selectingMode, setSelectingMode] = useState("start"); // 'start' or 'end'

  const onDateClick = (day) => {
    if (selectingMode === "start") {
      onChange(day, null); // Reset end date
      setSelectingMode("end");
    } else {
      // If clicking before start date, reset start
      if (startDate && isBefore(day, startDate)) {
        onChange(day, null);
        setSelectingMode("end");
      } else {
        onChange(startDate, day);
        setSelectingMode("start"); // Reset cycle
      }
    }
  };

  const nextMonth = () => setCurrentMonth(addMonths(currentMonth, 1));
  const prevMonth = () => setCurrentMonth(subMonths(currentMonth, 1));

  const renderMonth = (monthDate) => {
    const monthStart = startOfMonth(monthDate);
    const monthEnd = endOfMonth(monthStart);
    const startDateGrid = startOfWeek(monthStart, { weekStartsOn: 1 }); // Monday start
    const endDateGrid = endOfWeek(monthEnd, { weekStartsOn: 1 });

    const daysHeader = [];
    const dateLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    for (let i = 0; i < 7; i++) {
      daysHeader.push(
        <div
          key={i}
          className="text-center text-xs font-bold text-slate-400 py-3 uppercase tracking-wider"
        >
          {dateLabels[i]}
        </div>,
      );
    }

    const rows = [];
    let days = [];
    let day = startDateGrid;

    while (day <= endDateGrid) {
      for (let i = 0; i < 7; i++) {
        const cloneDay = day;
        const formattedDate = format(day, "d");

        const isSelectedStart = startDate && isSameDay(day, startDate);
        const isSelectedEnd = endDate && isSameDay(day, endDate);
        const isInRange =
          startDate &&
          endDate &&
          isWithinInterval(day, { start: startDate, end: endDate });
        const isCurrentMonth = isSameMonth(day, monthStart);
        const isPast = isBefore(day, new Date()) && !isSameDay(day, new Date());

        let cellClass =
          "relative w-full aspect-square flex items-center justify-center text-sm rounded-full transition-all cursor-pointer z-10 ";

        if (!isCurrentMonth) {
          cellClass += "invisible ";
        } else if (isSelectedStart || isSelectedEnd) {
          cellClass +=
            "bg-linear-to-r from-blue-500 to-cyan-500 text-white font-bold shadow-lg shadow-blue-500/30 transform scale-110 ";
        } else if (isInRange) {
          cellClass += "bg-blue-50 text-blue-600 font-medium ";
        } else if (isPast) {
          cellClass += "text-slate-300 cursor-not-allowed decoration-slice ";
        } else {
          cellClass +=
            "text-slate-700 hover:bg-blue-50 hover:text-blue-600 font-medium ";
        }

        // Range Connectors
        const isRangeStart = isSelectedStart && endDate;
        const isRangeEnd = isSelectedEnd && startDate;
        const isMiddle = isInRange && !isSelectedStart && !isSelectedEnd;

        days.push(
          <div
            key={format(cloneDay, "yyyy-MM-dd")}
            className={`relative p-0.5 ${!isCurrentMonth ? "opacity-0 pointer-events-none" : ""}`}
            onClick={() => !isPast && onDateClick(cloneDay)}
          >
            {(isRangeStart || isMiddle) && (
              <div className="absolute top-1/2 bottom-1/2 right-0 left-1/2 -translate-y-1/2 h-full bg-blue-50 z-0" />
            )}
            {(isRangeEnd || isMiddle) && (
              <div className="absolute top-1/2 bottom-1/2 left-0 right-1/2 -translate-y-1/2 h-full bg-blue-50 z-0" />
            )}
            <div className={cellClass}>{formattedDate}</div>
          </div>,
        );
        day = addDays(day, 1);
      }
      rows.push(
        <div
          className="grid grid-cols-7"
          key={`row-${format(day, "yyyy-MM-dd")}`}
        >
          {days}
        </div>,
      );
      days = [];
    }

    return (
      <div className="flex-1 w-full min-w-[250px] select-none">
        <div className="text-center mb-6">
          <span className="text-lg font-bold text-slate-800 capitalize block tracking-tight">
            {format(monthDate, "MMMM yyyy", { locale: vi })}
          </span>
        </div>
        <div className="grid grid-cols-7 mb-2 border-b border-slate-50 pb-2">
          {daysHeader}
        </div>
        <div>{rows}</div>
      </div>
    );
  };

  return (
    <div className="w-full">
      <div className="bg-white rounded-2xl border border-slate-100 p-3 md:p-5 shadow-sm flex flex-col md:flex-row gap-4 md:gap-6 relative">
        {/* Control Header */}
        <button
          onClick={prevMonth}
          className="absolute top-3 left-3 md:top-5 md:left-5 z-20 p-2 hover:bg-slate-50 rounded-full transition-colors border border-slate-200 shadow-sm bg-white text-slate-500 hover:text-slate-800"
        >
          <ChevronLeft size={18} />
        </button>

        <button
          onClick={nextMonth}
          className="absolute top-3 right-3 md:top-5 md:right-5 z-20 p-2 hover:bg-slate-50 rounded-full transition-colors border border-slate-200 shadow-sm bg-white text-slate-500 hover:text-slate-800"
        >
          <ChevronRight size={18} />
        </button>

        <div className="flex-1">{renderMonth(currentMonth)}</div>

        {/* Divider */}
        <div className="hidden md:block w-px bg-slate-100 my-2" />

        <div className="flex-1 hidden md:block">
          {renderMonth(addMonths(currentMonth, 1))}
        </div>
      </div>
    </div>
  );
};

export default DateRangePicker;
