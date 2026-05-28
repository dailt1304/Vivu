import React, { useState, useEffect, useCallback } from "react";
import { Switch } from "@/components/ui/switch";
import { ChevronDown, ChevronUp, ArrowRight, Clock } from "lucide-react";
import TimeInput from "./TimeInput";

export const DAYS_OF_WEEK = [
  { id: "monday", label: "Thứ Hai" },
  { id: "tuesday", label: "Thứ Ba" },
  { id: "wednesday", label: "Thứ Tư" },
  { id: "thursday", label: "Thứ Năm" },
  { id: "friday", label: "Thứ Sáu" },
  { id: "saturday", label: "Thứ Bảy" },
  { id: "sunday", label: "Chủ Nhật" },
];

const DEFAULT_DAY_SCHEDULE = { isOpen: true, is24h: false, open: "06:00", close: "22:00" };

const parseSchedule = (value) => {
  const defaultHours = DAYS_OF_WEEK.reduce((acc, day) => {
    acc[day.id] = { ...DEFAULT_DAY_SCHEDULE };
    return acc;
  }, {});

  if (!value) return defaultHours;

  try {
    if (typeof value === "string" && value.trim().startsWith("{")) {
      const raw = JSON.parse(value);
      const parsedHours = { ...defaultHours };
      Object.keys(raw).forEach((day) => {
        if (!DAYS_OF_WEEK.find(d => d.id === day)) return;
        if (raw[day] === "closed") {
          parsedHours[day] = { isOpen: false, is24h: false, open: "06:00", close: "22:00" };
        } else if (raw[day] === "Mở cửa cả ngày" || raw[day] === "24/24" || raw[day] === "Cả ngày") {
          parsedHours[day] = { isOpen: true, is24h: true, open: "06:00", close: "22:00" };
        } else {
          const [o, c] = raw[day].split("-");
          parsedHours[day] = {
            isOpen: true,
            is24h: false,
            open: o?.trim() || "06:00",
            close: c?.trim() || "22:00",
          };
        }
      });
      return parsedHours;
    } else if (typeof value === "string") {
      // It's a plain string like "07:00 - 21:00"
      let o = "06:00", c = "22:00";
      if (value.includes("-")) {
        const parts = value.split("-");
        o = parts[0].trim();
        c = parts[1].trim();
      }
      return DAYS_OF_WEEK.reduce((acc, day) => {
        acc[day.id] = { isOpen: true, is24h: false, open: o, close: c };
        return acc;
      }, {});
    }
  } catch (e) {
    console.error("Failed to parse opening hours", e);
  }
  return defaultHours;
};

const checkSameForAll = (schedule) => {
  const mon = schedule.monday;
  return DAYS_OF_WEEK.every(d => {
    const s = schedule[d.id];
    return s.isOpen === mon.isOpen && s.is24h === mon.is24h && s.open === mon.open && s.close === mon.close;
  });
};

const serializeSchedule = (schedule) => {
  const hoursJson = {};
  Object.keys(schedule).forEach((day) => {
    if (!schedule[day].isOpen) {
      hoursJson[day] = "closed";
    } else if (schedule[day].is24h) {
      hoursJson[day] = "Mở cửa cả ngày";
    } else {
      hoursJson[day] = `${schedule[day].open}-${schedule[day].close}`;
    }
  });
  return JSON.stringify(hoursJson);
};

const WeeklyScheduleInput = ({ 
  value, 
  onChange, 
  compact = true,
  variant = "user",
  themeColor = "blue"
}) => {
  const [schedule, setSchedule] = useState(() => parseSchedule(value));
  const [sameForAll, setSameForAll] = useState(() => checkSameForAll(parseSchedule(value)));
  const [expandedDay, setExpandedDay] = useState(null);

  const lastEmittedValue = React.useRef(value);

  const updateSchedule = useCallback((newSchedule) => {
    setSchedule(newSchedule);
    const newValue = serializeSchedule(newSchedule);
    lastEmittedValue.current = newValue;
    onChange(newValue);
  }, [onChange]);

  // Sync internal state when value changes from outside
  useEffect(() => {
    if (value === lastEmittedValue.current) {
      return;
    }
    const newSchedule = parseSchedule(value);
    setSchedule(newSchedule);
    setSameForAll(checkSameForAll(newSchedule));
    lastEmittedValue.current = value;
  }, [value]);

  const handleSameForAllToggle = (checked) => {
    setSameForAll(checked);
    if (checked) {
      // Sync all days to match Monday
      const monday = schedule.monday;
      const newSchedule = DAYS_OF_WEEK.reduce((acc, day) => {
        acc[day.id] = { ...monday };
        return acc;
      }, {});
      updateSchedule(newSchedule);
    }
  };

  const handleDayUpdate = (dayId, updates) => {
    let newSchedule = {
      ...schedule,
      [dayId]: { ...schedule[dayId], ...updates }
    };

    if (sameForAll) {
      // Sync all days to match this updated day
      const updatedDay = newSchedule[dayId];
      newSchedule = DAYS_OF_WEEK.reduce((acc, day) => {
        acc[day.id] = { ...updatedDay };
        return acc;
      }, {});
    } else {
      // If manually syncing back to same, maybe checkSameForAll? We can leave that to the toggle
    }

    updateSchedule(newSchedule);
  };

  const activeColor = themeColor === 'emerald' ? 'text-emerald-600' : 'text-blue-600';
  const activeBg = themeColor === 'emerald' ? 'bg-emerald-50 border-emerald-100' : 'bg-blue-50 border-blue-100';
  const activeFocus = themeColor === 'emerald' ? 'focus:ring-emerald-500' : 'focus:ring-blue-500';

  const CheckboxUI = ({ checked, onChange, label }) => (
    <label className={`flex items-center gap-1.5 text-xs font-medium cursor-pointer shrink-0 whitespace-nowrap sm:mr-2 ${activeColor}`}>
      <input 
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className={`rounded border-gray-300 ${activeColor} ${activeFocus} h-3.5 w-3.5`}
      />
      {label}
    </label>
  );

  const renderTimeInput = (dayId, daySchedule) => {
    return (
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-2 flex-1 w-full justify-end">
        {daySchedule.isOpen ? (
          <>
            <CheckboxUI 
              checked={daySchedule.is24h} 
              onChange={(e) => handleDayUpdate(dayId, { is24h: e.target.checked })} 
              label="Cả ngày" 
            />
            {daySchedule.is24h ? (
              <div className={`text-xs font-medium px-3 py-2 rounded-lg w-full sm:w-[190px] text-center border ${activeBg} ${activeColor}`}>
                Mở cửa cả ngày
              </div>
            ) : (
              <div className="flex items-center gap-2 w-full sm:w-auto">
                <TimeInput
                  value={daySchedule.open}
                  onChange={(val) => handleDayUpdate(dayId, { open: val })}
                  variant={variant}
                  themeColor={themeColor}
                />
                <span className="text-gray-400">-</span>
                <TimeInput
                  value={daySchedule.close}
                  onChange={(val) => handleDayUpdate(dayId, { close: val })}
                  variant={variant}
                  themeColor={themeColor}
                />
              </div>
            )}
          </>
        ) : (
          <div className="text-xs text-red-500 font-medium bg-red-50 px-3 py-2 rounded-lg w-full sm:w-[190px] text-center">
            Đóng cửa
          </div>
        )}
      </div>
    );
  };

  const wrapperClass = variant === "cms" ? "bg-white overflow-hidden rounded-xl border border-gray-200" : "bg-white dark:bg-gray-800 rounded-xl border border-gray-100 dark:border-gray-700 shadow-sm p-4";

  return (
    <div className={wrapperClass}>
      {/* Header with Switch */}
      <div className={`flex items-center justify-between ${variant === "cms" ? "p-3 border-b border-gray-100 bg-gray-50/50" : "mb-4"}`}>
        <div>
          <span className="text-sm font-semibold text-gray-900 dark:text-gray-100 block">
            Giờ hoạt động
          </span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {sameForAll ? "Giống nhau tất cả các ngày" : "Khác nhau theo từng ngày"}
          </span>
        </div>
        <Switch checked={sameForAll} onCheckedChange={handleSameForAllToggle} themeColor={themeColor} />
      </div>

      {sameForAll ? (
        // Same for all UI (Compact Time Range Input)
        <div className={`flex flex-wrap sm:flex-nowrap items-center gap-x-2 gap-y-4 sm:gap-4 ${variant === "cms" ? "p-3" : "pt-2"}`}>
          {variant !== "cms" && schedule.monday.isOpen && !schedule.monday.is24h && (
             <div className="flex-1 w-auto min-w-[100px]">
               <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 ml-1">Bắt đầu</p>
               <TimeInput
                 value={schedule.monday.open}
                 onChange={(val) => handleDayUpdate("monday", { open: val, isOpen: true })}
                 variant={variant}
                 themeColor={themeColor}
               />
             </div>
          )}
          {variant === "cms" && schedule.monday.isOpen && !schedule.monday.is24h && (
            <TimeInput
              value={schedule.monday.open}
              onChange={(val) => handleDayUpdate("monday", { open: val, isOpen: true })}
              variant={variant}
              themeColor={themeColor}
            />
          )}

          {(!schedule.monday.isOpen || schedule.monday.is24h) ? (
             <div className="w-full">
                {renderTimeInput("monday", schedule.monday)}
             </div>
          ) : (
            <>
              {variant !== "cms" && (
                <div className="pt-6 shrink-0 block">
                  <ArrowRight className="text-gray-300 dark:text-gray-600" size={16} />
                </div>
              )}
              {variant === "cms" && <span className="text-gray-400">-</span>}

              {variant !== "cms" ? (
                <div className="flex-1 w-auto min-w-[100px]">
                  <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 ml-1">Kết thúc</p>
                  <TimeInput
                    value={schedule.monday.close}
                    onChange={(val) => handleDayUpdate("monday", { close: val, isOpen: true })}
                    variant={variant}
                    themeColor={themeColor}
                  />
                </div>
              ) : (
                <TimeInput
                  value={schedule.monday.close}
                  onChange={(val) => handleDayUpdate("monday", { close: val, isOpen: true })}
                  variant={variant}
                  themeColor={themeColor}
                />
              )}
            </>
          )}
          
          {/* Include toggle for is24 / Closed if in user form and not currently showing them as override */}
          {variant !== "cms" && (!(!schedule.monday.isOpen || schedule.monday.is24h)) && (
            <div className="pt-0 sm:pt-6 w-full sm:w-auto flex justify-end shrink-0 sm:block">
               <CheckboxUI 
                 checked={schedule.monday.is24h} 
                 onChange={(e) => handleDayUpdate("monday", { is24h: e.target.checked })} 
                 label="Cả ngày" 
               />
            </div>
          )}
        </div>
      ) : (
         // Different per day UI
         <div className="divide-y divide-gray-100 dark:divide-gray-800">
           {DAYS_OF_WEEK.map((day, idx) => {
             const daySchedule = schedule[day.id];
             const isExpanded = expandedDay === day.id;

             // Mobile/Compact mode - Accordion style
             if (compact) {
               return (
                 <div key={day.id} className="py-2">
                   <div 
                     className="flex items-center justify-between p-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer transition-colors"
                     onClick={() => setExpandedDay(isExpanded ? null : day.id)}
                   >
                     <div className="flex items-center gap-3">
                       <Switch
                         checked={daySchedule.isOpen}
                         onCheckedChange={(checked) => handleDayUpdate(day.id, { isOpen: checked })}
                         onClick={(e) => e.stopPropagation()}
                         themeColor={themeColor}
                       />
                       <span className={`text-sm font-medium ${daySchedule.isOpen ? "text-gray-900 dark:text-gray-100" : "text-gray-400 dark:text-gray-500"}`}>
                         {day.label}
                       </span>
                     </div>
                     <div className="flex items-center gap-2">
                       <span className="text-xs text-gray-500">
                         {!daySchedule.isOpen ? "Đóng cửa" : daySchedule.is24h ? "Cả ngày" : `${daySchedule.open} - ${daySchedule.close}`}
                       </span>
                       {isExpanded ? <ChevronUp size={16} className="text-gray-400" /> : <ChevronDown size={16} className="text-gray-400" />}
                     </div>
                   </div>
                   {isExpanded && (
                     <div className="pt-2 pb-3 px-2">
                       {renderTimeInput(day.id, daySchedule)}
                     </div>
                   )}
                 </div>
               );
             }

             // CMS mode - List style
             return (
               <div
                 key={day.id}
                 className={`flex items-center flex-wrap sm:flex-nowrap justify-between p-3 gap-4 hover:bg-gray-50 transition-colors`}
               >
                 <div className="flex items-center gap-3 w-[140px]">
                   <Switch
                     checked={daySchedule.isOpen}
                     onCheckedChange={(checked) => handleDayUpdate(day.id, { isOpen: checked })}
                     themeColor={themeColor}
                   />
                   <span className={`text-sm font-medium ${daySchedule.isOpen ? "text-gray-900" : "text-gray-400"}`}>
                     {day.label}
                   </span>
                 </div>
                 {renderTimeInput(day.id, daySchedule)}
               </div>
             );
           })}
         </div>
      )}
    </div>
  );
};

export default WeeklyScheduleInput;
