import React, { useState, useEffect, useRef } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ChevronDown } from "lucide-react";

/**
 * A reusable 24-hour time picker with a dropdown for hours and minutes.
 * @param {string} value - The current time value in "HH:MM" format.
 * @param {function} onChange - Callback triggered when a new time is selected.
 * @param {string} label - Optional label to display above the input.
 */
export const TimePickerDropdown = ({ value, onChange, label }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);
  const [hour, minute] = value ? value.split(":") : ["00", "00"];

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const hoursAvailable = Array.from({ length: 24 }, (_, i) =>
    i.toString().padStart(2, "0"),
  );
  const minutesAvailable = Array.from({ length: 12 }, (_, i) =>
    (i * 5).toString().padStart(2, "0"),
  );

  const handleSelect = (newHour, newMinute) => {
    const h = newHour || hour || "00";
    const m = newMinute || minute || "00";
    onChange(`${h}:${m}`);
  };

  return (
    <div className="flex-1 flex flex-col relative" ref={dropdownRef}>
      {label && (
        <span className="text-xs text-slate-500 mb-1 block font-medium ml-1">
          {label}
        </span>
      )}
      <div
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-between bg-slate-50 border border-slate-200 rounded-xl px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 transition-all w-full cursor-pointer hover:bg-slate-100/50 min-h-[40px]"
      >
        <span className="text-sm font-bold text-slate-700">
          {hour} : {minute}
        </span>
        <ChevronDown
          size={14}
          className={`text-slate-400 transition-transform duration-300 ${isOpen ? "rotate-180" : ""}`}
        />
      </div>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            className="absolute top-full left-0 right-0 mt-2 bg-white rounded-2xl shadow-2xl border border-slate-100 overflow-hidden z-[100] p-4 flex gap-4 min-w-[200px] ring-1 ring-black/5"
          >
            <div className="flex-1">
              <div className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 px-1 text-center">
                Giờ
              </div>
              <div className="grid grid-cols-1 gap-1 max-h-[160px] overflow-y-auto pr-1 select-none hide-scrollbar scroll-smooth">
                {hoursAvailable.map((h) => (
                  <button
                    key={h}
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleSelect(h, minute);
                    }}
                    className={`px-2 py-1.5 text-xs font-bold rounded-lg transition-all duration-200 ${
                      hour === h
                        ? "bg-linear-to-r from-blue-500 to-cyan-500 text-white shadow-md shadow-blue-500/25 scale-[1.02]"
                        : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                    }`}
                  >
                    {h}
                  </button>
                ))}
              </div>
            </div>

            <div className="w-px bg-slate-100 my-1" />

            <div className="flex-1">
              <div className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 px-1 text-center">
                Phút
              </div>
              <div className="grid grid-cols-1 gap-1 max-h-[160px] overflow-y-auto pr-1 select-none hide-scrollbar scroll-smooth">
                {minutesAvailable.map((m) => (
                  <button
                    key={m}
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleSelect(hour, m);
                    }}
                    className={`px-2 py-1.5 text-xs font-bold rounded-lg transition-all duration-200 ${
                      minute === m
                        ? "bg-linear-to-r from-blue-500 to-cyan-500 text-white shadow-md shadow-blue-500/25 scale-[1.02]"
                        : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                    }`}
                  >
                    {m}
                  </button>
                ))}
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};
