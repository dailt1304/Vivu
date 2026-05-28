import React, { useState, useEffect, useRef } from "react";
import { MapPin, CalendarDays, Loader2, ArrowRight } from "lucide-react";
import { useSearchCities } from "../../../../hooks/cities/useCities";
import useClickOutside from "../../../../hooks/utils/useClickOutside";
import { format, differenceInDays } from "date-fns";
import { vi } from "date-fns/locale";
import * as Popover from "@radix-ui/react-popover";
import { motion, AnimatePresence } from "framer-motion";
import DateRangePicker from "../../../common/inputs/DateRangePicker";
import toast from "../../../../utils/toast";

const DestinationStep = ({ data, errors, setFormData, nextStep, tripLimitInfo }) => {
  const { destination, startDate, endDate } = data;
  const [localDateError, setLocalDateError] = useState("");

  const [searchTerm, setSearchTerm] = useState(
    typeof destination === "object" && destination !== null
      ? destination.name
      : destination || "",
  );
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [suggestions, setSuggestions] = useState([]);
  const [isDatePickerOpen, setIsDatePickerOpen] = useState(false);

  const { trigger: searchCities, isMutating } = useSearchCities();
  const suggestionsRef = useRef(null);

  useClickOutside(suggestionsRef, () => setShowSuggestions(false));

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(async () => {
      if (searchTerm.trim() && showSuggestions) {
        try {
          const res = await searchCities({
            searchText: searchTerm,
            pageSize: 5,
          });
          if (res?.items) {
            setSuggestions(res.items);
          }
        } catch (error) {
          console.error("Error searching cities", error);
        }
      } else {
        setSuggestions([]);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [searchTerm, searchCities, showSuggestions]);

  const handleSelectCity = (city) => {
    let pickedUrl = null;
    if (city.image) {
      try {
        const parsed = typeof city.image === 'string' ? JSON.parse(city.image) : city.image;
        if (Array.isArray(parsed) && parsed.length > 0) {
          pickedUrl = parsed[Math.floor(Math.random() * parsed.length)];
        } else {
          pickedUrl = typeof parsed === 'string' ? parsed : parsed?.url || city.image;
        }
      } catch {
        pickedUrl = city.image;
      }
    }

    setSearchTerm(city.name);
    setFormData({ 
      destination: city, 
      selectedCoverUrl: pickedUrl 
    });
    setShowSuggestions(false);
  };

  const handleSearchChange = (e) => {
    const val = e.target.value;
    setSearchTerm(val);
    setFormData({ destination: val });
    setShowSuggestions(true);
  };

  // Min date for start date is today

  const duration =
    startDate && endDate && startDate <= endDate
      ? differenceInDays(endDate, startDate)
      : 0;

  const handleNext = () => {
    if (tripLimitInfo?.hasReachedLimit) {
      toast.error(
        `Bạn đã đạt giới hạn tạo chuyến đi (${tripLimitInfo.numberOfTripCreated}/${tripLimitInfo.tripLimit}). Nâng cấp Premium để tạo không giới hạn!`
      );
      return;
    }
    
    if (startDate && endDate && duration <= 0) {
      setLocalDateError("Chuyến đi phải kéo dài ít nhất 1 ngày (đi và về khác ngày).");
      return;
    }
    setLocalDateError("");
    nextStep();
  };

  return (
    <div className="space-y-6 animate-in fade-in slide-in-from-right-4 duration-500">
      <div className="space-y-2">
        <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight">
          Lên kế hoạch chuyến đi
        </h2>
        <p className="text-slate-500 font-medium">
          Bắt đầu bằng việc chọn điểm đến và thời gian lý tưởng cho bạn.
        </p>
      </div>

      <div className="space-y-5">
        {/* Destination Field */}
        <div className="space-y-2 relative" ref={suggestionsRef}>
          <label className="text-sm font-bold text-slate-700 block transition-colors">
            Điểm đến <span className="text-red-500">*</span>
          </label>
          <div className="relative group">
            <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400 group-focus-within:text-blue-500 transition-colors">
              <MapPin size={18} />
            </div>
            <input
              type="text"
              placeholder="Ví dụ: Đà Lạt, Phú Quốc, Hà Nội..."
              value={searchTerm}
              onChange={handleSearchChange}
              onFocus={() => setShowSuggestions(true)}
              className={`w-full pl-11 pr-4 py-3.5 bg-slate-50 hover:bg-slate-100 focus:bg-white border ${errors.destination ? "border-red-300 focus:ring-red-100" : "border-slate-200 focus:ring-blue-100 focus:border-blue-400"} rounded-2xl text-sm font-medium text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 transition-all shadow-sm`}
            />
            {isMutating && (
              <div className="absolute inset-y-0 right-0 pr-3.5 flex items-center pointer-events-none">
                <Loader2 size={16} className="animate-spin text-slate-400" />
              </div>
            )}
          </div>
          {errors.destination && (
            <p className="text-xs font-semibold text-red-500 mt-1">
              {errors.destination}
            </p>
          )}

          {/* Suggestions Dropdown */}
          {showSuggestions && suggestions.length > 0 && (
            <div className="absolute z-50 w-full mt-1 bg-white rounded-xl shadow-lg border border-slate-100 py-1 overflow-hidden animate-in fade-in zoom-in-95">
              {suggestions.map((city) => (
                <button
                  key={city.id}
                  type="button"
                  onClick={() => handleSelectCity(city)}
                  className="w-full text-left px-4 py-3 hover:bg-blue-50 flex items-center gap-3 transition-colors"
                >
                  <div className="bg-slate-100 p-2 rounded-lg text-slate-500">
                    <MapPin size={16} />
                  </div>
                  <div>
                    <span className="block text-sm font-bold text-slate-800">
                      {city.name}
                    </span>
                    {city.countryName && (
                      <span className="block text-xs font-medium text-slate-500">
                        {city.countryName}
                      </span>
                    )}
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Dates Field (Radix Popover) */}
        <div className="space-y-2">
          <label className="text-sm font-bold text-slate-700 block">
            Thời gian chuyến đi <span className="text-red-500">*</span>
          </label>
          <Popover.Root
            open={isDatePickerOpen}
            onOpenChange={setIsDatePickerOpen}
          >
            <Popover.Trigger asChild>
              <button
                type="button"
                className={`w-full flex md:flex-row flex-col items-center justify-between p-2 md:p-4 bg-slate-50 hover:bg-slate-100 border ${errors.startDate || errors.endDate ? "border-red-300 ring-1 ring-red-100 bg-red-50/30" : "border-slate-200"} rounded-2xl transition-all shadow-sm group active:scale-[0.99] focus:outline-none focus:ring-2 focus:ring-blue-100`}
              >
                <div className="flex items-center gap-3 w-full md:w-auto">
                  <div className="w-10 h-10 shrink-0 rounded-xl bg-white shadow-sm flex items-center justify-center text-blue-500 group-hover:text-blue-600 transition-colors border border-slate-100">
                    <CalendarDays size={18} />
                  </div>
                  <div className="flex flex-col items-start gap-0.5">
                    <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                      Ngày đi
                    </span>
                    <span className="text-sm font-bold text-slate-800">
                      {startDate
                        ? format(startDate, "dd/MM/yyyy", { locale: vi })
                        : "Chưa chọn"}
                    </span>
                  </div>
                </div>

                <div className="py-2 md:py-0 px-4 text-slate-300 rotate-90 md:rotate-0">
                  <ArrowRight size={16} strokeWidth={2.5} />
                </div>

                <div className="flex items-center gap-3 w-full md:w-auto md:justify-end justify-start">
                  <div className="w-10 h-10 md:hidden shrink-0 rounded-xl bg-white shadow-sm flex items-center justify-center text-cyan-500 group-hover:text-cyan-600 transition-colors border border-slate-100">
                    <CalendarDays size={18} />
                  </div>
                  <div className="flex flex-col items-start md:items-end gap-0.5 flex-1">
                    <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                      Ngày về
                    </span>
                    <span className="text-sm font-bold text-slate-800">
                      {endDate
                        ? format(endDate, "dd/MM/yyyy", { locale: vi })
                        : "Chưa chọn"}
                    </span>
                  </div>
                  <div className="w-10 h-10 hidden md:flex shrink-0 items-center justify-center rounded-xl bg-white shadow-sm text-cyan-500 group-hover:text-cyan-600 transition-colors border border-slate-100">
                    <CalendarDays size={18} />
                  </div>
                </div>
              </button>
            </Popover.Trigger>

            <AnimatePresence>
              {isDatePickerOpen && (
                <Popover.Portal forceMount>
                  <Popover.Content
                    align="center"
                    sideOffset={8}
                    collisionPadding={16}
                    asChild
                    className="z-[12000] outline-none"
                  >
                    <motion.div
                      initial={{ opacity: 0, y: 10, scale: 0.95 }}
                      animate={{ opacity: 1, y: 0, scale: 1 }}
                      exit={{
                        opacity: 0,
                        scale: 0.95,
                        transition: { duration: 0.15 },
                      }}
                      transition={{
                        type: "spring",
                        damping: 25,
                        stiffness: 300,
                      }}
                    >
                      <div className="w-[calc(100vw-32px)] md:w-auto max-w-3xl overflow-x-auto overflow-y-hidden scrollbar-hide p-1">
                        <DateRangePicker
                          startDate={startDate}
                          endDate={endDate}
                          onChange={(start, end) => {
                            setFormData({ startDate: start, endDate: end });
                            setLocalDateError("");
                            if (start && end) {
                              setTimeout(() => setIsDatePickerOpen(false), 300);
                            }
                          }}
                        />
                      </div>
                    </motion.div>
                  </Popover.Content>
                </Popover.Portal>
              )}
            </AnimatePresence>
          </Popover.Root>
          {(errors.startDate || errors.endDate || localDateError) && (
            <p className="text-xs font-semibold text-red-500 mt-1">
              {localDateError || errors.startDate || errors.endDate}
            </p>
          )}
        </div>
      </div>

      {duration > 0 && (
        <div className="bg-blue-50/40 border border-blue-100/50 rounded-2xl p-4 flex items-center justify-center animate-in fade-in slide-in-from-bottom-2 shadow-sm ring-1 ring-blue-500/5">
          <p className="text-sm font-extrabold bg-gradient-primary bg-clip-text text-transparent tracking-wide">
            Tổng thời gian chuyến đi: {duration} ngày
          </p>
        </div>
      )}

      <div className="pt-4 mt-8 border-t border-slate-100">
        <button
          onClick={handleNext}
          className="w-full sm:w-auto ml-auto px-8 py-3.5 bg-gradient-primary text-white font-bold rounded-2xl shadow-lg shadow-blue-200 hover:shadow-xl hover:-translate-y-0.5 active:translate-y-0 transition-all flex items-center justify-center gap-2"
        >
          Tiếp tục
          <ArrowRight size={18} strokeWidth={2.5} />
        </button>
      </div>
    </div>
  );
};

export default DestinationStep;
