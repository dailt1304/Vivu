import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  X,
  Calendar as CalendarIcon,
  MapPin,
  ChevronLeft,
  ChevronRight,
  Loader2,
} from "lucide-react";
import {
  format,
  addMonths,
  subMonths,
  startOfMonth,
  endOfMonth,
  startOfWeek,
  endOfWeek,
  isSameMonth,
  isSameDay,
  isWithinInterval,
  isBefore,
  startOfDay,
} from "date-fns";
import { vi } from "date-fns/locale";
import { useSearchCities } from "../../../hooks/cities/useCities";
import useDebounce from "../../../hooks/utils/useDebounce";

const DEFAULT_TRIP_IMAGE =
  "https://images.unsplash.com/photo-1555921015-5532091f6026?q=80&w=1470&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D";

const CreateTripModal = ({ isOpen, onClose, onCreate }) => {
  const [tripName, setTripName] = useState("");
  const [location, setLocation] = useState("");
  const [selectedCity, setSelectedCity] = useState(null);
  const [participants, setParticipants] = useState(1);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [selectedCityImage, setSelectedCityImage] =
    useState(DEFAULT_TRIP_IMAGE);

  // Search Logic
  const debouncedLocation = useDebounce(location, 400);
  const {
    trigger: searchCities,
    data: searchResult,
    isMutating: isSearching,
  } = useSearchCities();

  const cities = searchResult?.items || [];

  useEffect(() => {
    if (debouncedLocation && debouncedLocation.length >= 2 && !selectedCity) {
      searchCities({
        searchText: debouncedLocation,
        pageNumber: 1,
        pageSize: 10,
      });
    }
  }, [debouncedLocation, searchCities, selectedCity]);

  // Calendar State
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [startDate, setStartDate] = useState(null);
  const [endDate, setEndDate] = useState(null);

  // Calendar Logic
  const handlePrevMonth = () => setCurrentMonth(subMonths(currentMonth, 1));
  const handleNextMonth = () => setCurrentMonth(addMonths(currentMonth, 1));

  const [dateError, setDateError] = useState("");

  const handleDateClick = (date) => {
    // Current local time
    const today = startOfDay(new Date());
    // Backend requires "Start date must be in the future"
    // Usually means strictly after "now". To be safe with only dates, "tomorrow" is the best future.
    const tomorrow = new Date(today);
    tomorrow.setDate(today.getDate() + 1);

    if (isBefore(date, tomorrow)) {
      setDateError("Ngày bắt đầu phải là từ ngày mai trở đi");
      return;
    }

    setDateError(""); // Clear any previous error when a valid date is selected

    if (!startDate || (startDate && endDate)) {
      setStartDate(date);
      setEndDate(null);
    } else if (startDate && !endDate) {
      if (isSameDay(date, startDate)) {
        setDateError("Ngày kết thúc phải sau ngày bắt đầu");
        return;
      }
      if (isBefore(date, startDate)) {
        setStartDate(date);
      } else {
        setEndDate(date);
      }
    }
  };

  const isSelected = (date) => {
    if (startDate && isSameDay(date, startDate)) return true;
    if (endDate && isSameDay(date, endDate)) return true;
    if (startDate && endDate) {
      return isWithinInterval(date, { start: startDate, end: endDate });
    }
    return false;
  };

  const isRangeStart = (date) => startDate && isSameDay(date, startDate);
  const isRangeEnd = (date) => endDate && isSameDay(date, endDate);

  const renderCalendar = () => {
    const monthStart = startOfMonth(currentMonth);
    const monthEnd = endOfMonth(monthStart);
    const startDateGrid = startOfWeek(monthStart, { weekStartsOn: 1 });
    const endDateGrid = endOfWeek(monthEnd, { weekStartsOn: 1 });

    const dateFormat = "d";
    const weekDays = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

    // Generate days array
    const calendarDays = [];
    let currentDay = startDateGrid;

    while (currentDay <= endDateGrid) {
      calendarDays.push(currentDay);
      currentDay = new Date(currentDay.getTime() + 24 * 60 * 60 * 1000);
    }

    return (
      <div className="w-full">
        {/* Calendar Header */}
        <div className="flex items-center justify-between mb-6 px-2">
          <button
            onClick={handlePrevMonth}
            className="p-2 hover:bg-cyan-50 text-gray-400 hover:text-cyan-600 rounded-xl transition-all"
          >
            <ChevronLeft size={20} />
          </button>
          <span className="font-bold text-lg text-gray-800 capitalize tracking-tight font-display">
            {format(currentMonth, "MMMM yyyy", { locale: vi })}
          </span>
          <button
            onClick={handleNextMonth}
            className="p-2 hover:bg-cyan-50 text-gray-400 hover:text-cyan-600 rounded-xl transition-all"
          >
            <ChevronRight size={20} />
          </button>
        </div>

        {/* Week Days */}
        <div className="grid grid-cols-7 mb-2">
          {weekDays.map((d) => (
            <div
              key={d}
              className="text-center text-xs font-bold text-gray-400/80 py-2 uppercase tracking-wider"
            >
              {d}
            </div>
          ))}
        </div>

        {/* Days Grid */}
        <div className="grid grid-cols-7 gap-y-2">
          {calendarDays.map((date) => {
            const formattedDate = format(date, dateFormat);
            const isCurrentMonth = isSameMonth(date, monthStart);
            const selected = isSelected(date);
            const rangeStart = isRangeStart(date);
            const rangeEnd = isRangeEnd(date);

            const today = startOfDay(new Date());
            const tomorrow = new Date(today);
            tomorrow.setDate(today.getDate() + 1);
            const isDisabled = isBefore(date, tomorrow);

            return (
              <div
                key={date.toString()}
                className="relative flex items-center justify-center h-9 w-full"
              >
                {/* Selection Background Range */}
                {selected && !rangeStart && !rangeEnd && (
                  <div className="absolute inset-0 bg-cyan-50 mx-[-4px]" />
                )}
                {/* Range Start/End Connectors */}
                {rangeStart && endDate && (
                  <div className="absolute top-0 bottom-0 right-0 left-1/2 bg-cyan-50" />
                )}
                {rangeEnd && startDate && (
                  <div className="absolute top-0 bottom-0 left-0 right-1/2 bg-cyan-50" />
                )}

                {/* Day Circle */}
                <div
                  className={`
                                    relative w-8 h-8 flex items-center justify-center text-sm font-medium rounded-full transition-all z-10
                                    ${isDisabled ? "text-gray-200 cursor-not-allowed" : "cursor-pointer"}
                                    ${!isDisabled && !isCurrentMonth ? "text-gray-300" : ""}
                                    ${!isDisabled && isCurrentMonth && !selected ? "text-gray-700 hover:bg-cyan-50 hover:text-cyan-600" : ""}
                                    ${selected && !rangeStart && !rangeEnd ? "text-cyan-600 font-bold" : ""}
                                    ${rangeStart || rangeEnd ? "bg-gradient-primary text-white shadow-md shadow-cyan-200 transform scale-105" : ""}
                                `}
                  onClick={() => !isDisabled && handleDateClick(date)}
                >
                  {formattedDate}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  const handleLocationSelect = (city) => {
    setLocation(city.name);
    setSelectedCity(city);
    
    let finalImage = DEFAULT_TRIP_IMAGE;
    const rawImage = city.imageUrl || city.image;
    
    if (rawImage) {
      try {
        let imageArray = rawImage;
        if (typeof rawImage === 'string' && rawImage.startsWith('[')) {
          imageArray = JSON.parse(rawImage);
        } else if (typeof rawImage === 'string') {
          finalImage = rawImage;
          imageArray = null;
        }
        
        if (Array.isArray(imageArray) && imageArray.length > 0) {
          const randomIndex = Math.floor(Math.random() * imageArray.length);
          finalImage = imageArray[randomIndex];
        }
      } catch (error) {
        console.error("Error parsing city images:", error);
      }
    }

    setSelectedCityImage(finalImage);
    setShowSuggestions(false);
  };

  const handleLocationChange = (e) => {
    const value = e.target.value;
    setLocation(value);
    setSelectedCity(null); // Clear selected city when typing new search
    setShowSuggestions(true);
    if (!value) setSelectedCityImage(DEFAULT_TRIP_IMAGE);
  };

  const handleSubmit = () => {
    // Final check before submission
    if (dateError || !location || !startDate || !endDate) return;

    const finalTitle = tripName.trim() 
      ? tripName 
      : `Chuyến đi đến ${selectedCity?.name || location}`;

    const newTrip = {
      title: finalTitle,
      description: `Chuyến đi đến ${selectedCity?.name || location}`,
      startDate: startDate ? startDate.toISOString() : null,
      endDate: endDate ? endDate.toISOString() : null,
      coverUrl: selectedCityImage,
      cityId: selectedCity?.id || null,
      tripSize: participants || 1,
      isPublic: false,
      generateInviteCode: true,
    };
    onCreate(newTrip);
    resetForm();
  };

  const resetForm = () => {
    setTripName("");
    setLocation("");
    setSelectedCity(null);
    setParticipants(1);
    setSelectedCityImage(DEFAULT_TRIP_IMAGE);
    setStartDate(null);
    setEndDate(null);
    setDateError("");
    onClose();
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-70 flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="absolute inset-0 bg-black/60 backdrop-blur-md"
          />

          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 30 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 30 }}
            className="relative w-full max-w-4xl bg-white rounded-4xl shadow-2xl overflow-hidden flex flex-col md:flex-row max-h-[90vh] md:h-auto"
          >
            {/* LEFT PANEL: Inputs & Info (Dynamic Background) */}
            <div className="w-full md:w-5/12 relative flex flex-col overflow-hidden transition-all duration-700 ease-in-out">
              {/* Background Image Layer */}
              <div className="absolute inset-0 z-0">
                <img
                  src={selectedCityImage || DEFAULT_TRIP_IMAGE}
                  alt="Background"
                  className="w-full h-full object-cover transition-transform duration-700 hover:scale-105"
                />
                {/* Gradient Overlays for Readability */}
                <div className="absolute inset-0 bg-linear-to-b from-black/40 via-black/20 to-black/80"></div>
                {/* Blue tint fallback if no specific image */}
                {selectedCityImage === DEFAULT_TRIP_IMAGE && (
                  <div className="absolute inset-0 bg-blue-600/20 mix-blend-overlay"></div>
                )}
              </div>

              {/* Content Container */}
              <div className="relative z-10 p-8 flex flex-col h-full text-white">
                {/* Header */}
                <div className="mb-8">
                  <button
                    onClick={onClose}
                    className="md:hidden absolute -top-2 -right-2 p-2 bg-white/20 rounded-full text-white"
                  >
                    <X size={20} />
                  </button>
                  <h2 className="text-3xl font-black tracking-tight font-display mb-2 drop-shadow-lg">
                    Tạo chuyến đi
                  </h2>
                  <p className="text-white/95 font-medium text-lg font-cursive leading-relaxed drop-shadow-md italic">
                    "Mỗi chuyến đi là một câu chuyện mới..."
                  </p>
                </div>

                {/* Inputs Stack */}
                <div className="space-y-6 flex-1">
                  {/* Participants & Trip Name Group */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {/* Trip Name */}
                    <div className="md:col-span-2 space-y-2 group">
                      <label className="text-xs font-bold text-blue-100 uppercase tracking-wider ml-1 drop-shadow-md font-display flex items-center justify-between">
                        <span>Tên chuyến đi</span>
                        <span className="text-white/50 text-[10px] font-normal normal-case tracking-normal">Không bắt buộc</span>
                      </label>
                      <input
                        type="text"
                        value={tripName}
                        onChange={(e) => setTripName(e.target.value)}
                        placeholder={selectedCity ? `VD: Chuyến đi đến ${selectedCity.name}` : "VD: Kỳ nghỉ hè tuyệt vời"}
                        className="w-full p-4 bg-white/20 border border-white/30 backdrop-blur-md focus:bg-white/30 rounded-2xl focus:outline-none focus:ring-2 focus:ring-white/50 transition-all font-bold text-white placeholder:text-white/60 font-display"
                      />
                    </div>

                    {/* Participants */}
                    <div className="space-y-2 group">
                      <label className="text-xs font-bold text-blue-100 uppercase tracking-wider ml-1 drop-shadow-md font-display">
                        Số người
                      </label>
                      <input
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        value={participants}
                        onChange={(e) => {
                          const val = e.target.value;
                          // Allow empty or positive integers only
                          if (val === "" || /^[1-9]\d*$/.test(val)) {
                            setParticipants(val === "" ? "" : parseInt(val));
                          }
                        }}
                        onBlur={() => {
                          // Reset to 1 if empty or invalid on blur
                          if (!participants || participants < 1) {
                            setParticipants(1);
                          }
                        }}
                        className="w-full p-4 bg-white/20 border border-white/30 backdrop-blur-md focus:bg-white/30 rounded-2xl focus:outline-none focus:ring-2 focus:ring-white/50 transition-all font-bold text-white text-center font-display"
                      />
                    </div>
                  </div>

                  {/* Location Search */}
                  <div className="space-y-2 relative">
                    <label className="text-xs font-bold text-blue-100 uppercase tracking-wider ml-1 drop-shadow-md font-display">
                      Điểm đến
                    </label>
                    <div className="relative group">
                      <MapPin className="absolute left-4 top-1/2 -translate-y-1/2 text-white/80 w-5 h-5 z-10" />
                      <input
                        type="text"
                        value={location}
                        onChange={handleLocationChange}
                        onFocus={() => setShowSuggestions(true)}
                        placeholder="Tìm kiếm thành phố..."
                        className="w-full pl-12 pr-4 py-4 bg-white/20 border border-white/30 backdrop-blur-md focus:bg-white/30 rounded-2xl focus:outline-none focus:ring-2 focus:ring-white/50 transition-all font-bold text-white placeholder:text-white/60 font-display"
                      />
                      {isSearching && (
                        <Loader2 className="absolute right-12 top-1/2 -translate-y-1/2 text-white/80 w-5 h-5 animate-spin" />
                      )}
                      {location && (
                        <button
                          onClick={() => {
                            setLocation("");
                            setSelectedCity(null);
                            setSelectedCityImage(DEFAULT_TRIP_IMAGE);
                          }}
                          className="absolute right-4 top-1/2 -translate-y-1/2 p-1 bg-white/20 rounded-full hover:bg-white/40 text-white transition-colors"
                        >
                          <X size={14} />
                        </button>
                      )}
                    </div>

                    {/* Suggestions Dropdown (Rich with API Results) */}
                    <AnimatePresence>
                      {showSuggestions &&
                        location.length >= 2 &&
                        !selectedCity && (
                          <motion.div
                            initial={{ opacity: 0, y: 10, scale: 0.95 }}
                            animate={{ opacity: 1, y: 0, scale: 1 }}
                            exit={{ opacity: 0, y: 10, scale: 0.95 }}
                            className="absolute z-50 top-full left-0 right-0 mt-2 bg-white rounded-2xl shadow-xl max-h-64 overflow-y-auto no-scrollbar py-2 border border-gray-100"
                          >
                            {cities.map((city, index) => (
                              <div
                                key={city.id || index}
                                onClick={() => handleLocationSelect(city)}
                                className="px-4 py-3 hover:bg-cyan-50 cursor-pointer text-gray-700 font-bold flex items-center gap-4 transition-colors group/item"
                              >
                                <div className="w-12 h-12 rounded-xl bg-cyan-100 flex items-center justify-center text-cyan-600 group-hover/item:scale-110 transition-transform overflow-hidden">
                                  <MapPin size={24} />
                                </div>
                                <div className="flex flex-col">
                                  <span className="text-gray-800 font-display">
                                    {city.name}
                                  </span>
                                  <span className="text-xs text-gray-400 font-normal font-display">
                                    {city.country?.name || "Việt Nam"}
                                  </span>
                                </div>
                              </div>
                            ))}
                            {cities.length === 0 && !isSearching && (
                              <div className="p-4 text-center text-gray-400 text-sm font-display">
                                Không tìm thấy địa điểm
                              </div>
                            )}
                            {isSearching && (
                              <div className="p-4 flex items-center justify-center">
                                <Loader2 className="w-6 h-6 animate-spin text-cyan-600" />
                              </div>
                            )}
                          </motion.div>
                        )}
                    </AnimatePresence>
                  </div>

                  {/* Selected Date Summary */}
                  {startDate && endDate && (
                    <div className="mt-auto pt-4 border-t border-white/20">
                      <div className="p-4 bg-black/30 backdrop-blur-md rounded-2xl border border-white/10">
                        <div className="flex items-center gap-2 text-blue-200 text-xs font-bold uppercase mb-1 font-display">
                          <CalendarIcon size={12} />
                          Thời gian đã chọn
                        </div>
                        <div className="text-white text-lg font-bold font-display">
                          {format(startDate, "dd/MM")} -{" "}
                          {format(endDate, "dd/MM/yyyy")}
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* RIGHT PANEL: Calendar & Actions (White Theme) */}
            <div className="w-full md:w-7/12 bg-white p-8 flex flex-col h-full md:h-auto z-10">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2 font-display">
                  <CalendarIcon className="text-cyan-600" size={20} />
                  Chọn ngày khởi hành
                </h3>
                <button
                  onClick={onClose}
                  className="hidden md:block p-2 hover:bg-gray-100 rounded-full text-gray-400 transition-colors"
                >
                  <X size={20} />
                </button>
              </div>

              <div className="flex-1 md:flex-none overflow-y-auto md:overflow-visible no-scrollbar py-2">
                {renderCalendar()}
              </div>

              {/* Error Message Display */}
              {dateError && (
                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="mt-2 p-3 bg-red-50 border border-red-100 rounded-xl text-red-600 text-sm font-bold flex items-center gap-2"
                >
                  <div className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse" />
                  {dateError}
                </motion.div>
              )}

              <div className="pt-6 border-t border-gray-100 mt-4">
                <button
                  onClick={handleSubmit}
                  disabled={
                    !location ||
                    !startDate ||
                    !endDate ||
                    dateError
                  }
                  className="w-full py-4 bg-gradient-primary text-white rounded-2xl font-bold text-lg shadow-xl shadow-cyan-200 hover:shadow-2xl hover:scale-[1.02] active:scale-[0.98] transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 font-display"
                >
                  Tạo chuyến đi
                </button>
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default CreateTripModal;
