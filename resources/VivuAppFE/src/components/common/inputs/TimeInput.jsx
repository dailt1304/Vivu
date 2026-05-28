import React, { useState, useRef, useEffect } from "react";
import { Clock, ChevronDown } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";

const TimeInput = ({ 
  value, 
  onChange, 
  placeholder = "--:--",
  variant = "user", // "user" (default) or "cms",
  themeColor = "blue" // "blue" or "emerald"
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);
  const [hour, minute] = value ? value.split(":") : ["", ""];

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const hours = Array.from({ length: 24 }, (_, i) =>
    i.toString().padStart(2, "0")
  );
  const minutes = Array.from({ length: 12 }, (_, i) =>
    (i * 5).toString().padStart(2, "0")
  );

  const handleSelect = (newHour, newMinute) => {
    // If we only have one part selected, we default the other to "00"
    const h = newHour || hour || "00";
    const m = newMinute || minute || "00";
    onChange(`${h}:${m}`);
  };

  // Styling maps
  const styles = {
    user: {
      wrapper: "w-full bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl pl-9 sm:pl-11 pr-2 sm:pr-3 py-2 sm:py-3 text-xs sm:text-sm font-semibold text-gray-900 dark:text-white focus-within:ring-2 focus-within:ring-blue-500/20 focus-within:border-blue-500 transition-all cursor-pointer flex items-center hover:bg-gray-100 dark:hover:bg-gray-800",
      icon: "text-blue-500",
      dropdown: "mt-2 bg-white/80 dark:bg-gray-900/80 backdrop-blur-xl rounded-2xl shadow-2xl border border-white/20 dark:border-gray-800 p-3 min-w-[200px] -translate-x-1/4 md:translate-x-0",
      header: "text-blue-500/50",
      item: "px-2 py-2 text-xs font-bold rounded-lg transition-all",
      itemActive: "bg-blue-500 text-white shadow-lg shadow-blue-500/30",
      itemInactive: "text-gray-600 dark:text-gray-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:text-blue-600",
      divider: "bg-gray-100 dark:bg-gray-800"
    },
    cms: {
      wrapper: `w-full bg-white border border-gray-200 rounded-lg px-3 py-2 text-sm font-semibold text-gray-900 focus-within:ring-2 transition-all cursor-pointer flex items-center justify-between hover:bg-gray-50 h-10 shadow-sm ${
        themeColor === 'emerald' 
          ? "focus-within:ring-emerald-500/20 focus-within:border-emerald-500" 
          : "focus-within:ring-blue-500/20 focus-within:border-blue-500"
      }`,
      icon: "text-gray-400 w-4 h-4 ml-2",
      dropdown: "mt-1 bg-white rounded-xl shadow-xl border border-gray-100 p-2 min-w-[160px]",
      header: "text-[10px] font-black text-gray-400",
      item: "px-2 py-1.5 text-xs font-semibold tabular-nums rounded-md transition-all",
      itemActive: themeColor === 'emerald' 
        ? "bg-emerald-50 text-emerald-600" 
        : "bg-blue-50 text-blue-600",
      itemInactive: "text-gray-600 hover:bg-gray-100",
      divider: "bg-gray-100"
    }
  };

  const currentStyles = styles[variant] || styles.user;

  return (
    <div className="relative w-full" ref={dropdownRef}>
      <div
        onClick={() => setIsOpen(!isOpen)}
        className={currentStyles.wrapper}
      >
        {variant === "user" ? (
          <>
            <div className={`absolute left-3 sm:left-4 top-1/2 -translate-y-1/2 pointer-events-none ${currentStyles.icon}`}>
              <Clock size={16} className="stroke-[2.5] w-3.5 h-3.5 sm:w-4 sm:h-4" />
            </div>
            <span className={!value ? "text-gray-400 font-normal" : ""}>
              {value || placeholder}
            </span>
            <ChevronDown
              size={14}
              className={`ml-auto text-gray-400 transition-transform duration-300 ${isOpen ? "rotate-180" : ""}`}
            />
          </>
        ) : (
          <>
            <span className={!value ? "text-gray-400 font-normal" : "tabular-nums"}>
              {value || placeholder}
            </span>
            <Clock className={currentStyles.icon} />
          </>
        )}
      </div>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 10 }}
            className={`absolute top-full right-0 left-0 overflow-hidden z-[100] flex gap-2 ${currentStyles.dropdown} ${variant === "cms" ? "right-0 left-auto" : ""}`}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Hours Column */}
            <div className="flex-1">
              <div className={`uppercase tracking-widest text-center mb-2 ${currentStyles.header}`}>
                Giờ
              </div>
              <div className="grid grid-cols-1 gap-1 max-h-[150px] md:max-h-[220px] overflow-y-auto pr-1 select-none hide-scrollbar scroll-smooth">
                {hours.map((h) => (
                  <button
                    key={h}
                    onClick={() => handleSelect(h, minute)}
                    className={`${currentStyles.item} ${
                      hour === h ? currentStyles.itemActive : currentStyles.itemInactive
                    }`}
                  >
                    {h}
                  </button>
                ))}
              </div>
            </div>

            <div className={`w-px my-2 ${currentStyles.divider}`} />

            {/* Minutes Column */}
            <div className="flex-1">
              <div className={`uppercase tracking-widest text-center mb-2 ${currentStyles.header}`}>
                Phút
              </div>
              <div className="grid grid-cols-1 gap-1 max-h-[150px] md:max-h-[220px] overflow-y-auto pr-1 select-none hide-scrollbar scroll-smooth">
                {minutes.map((m) => (
                  <button
                    key={m}
                    onClick={() => handleSelect(hour, m)}
                    className={`${currentStyles.item} ${
                      minute === m ? currentStyles.itemActive : currentStyles.itemInactive
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

export default TimeInput;
