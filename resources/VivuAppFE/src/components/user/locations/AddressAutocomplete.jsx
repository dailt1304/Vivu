import React, { useState, useRef, useEffect } from "react";
import { Search, MapPin, Loader2, Navigation } from "lucide-react";
import { useGoongAutocomplete } from "../../../hooks/maps/useGoongAutocomplete";
import goongApi from "../../../api/goongApi";

// Note: To avoid prop drilling, we could pass it or just make it standalone
// Here we assume it accesses SubmitContext directly as per Vercel's compound component pattern, or receives props
const AddressAutocomplete = ({ value, onChange, onSelectPlace, error }) => {
  const [isOpen, setIsOpen] = useState(false);
  // Initialize with UUID to avoid setState in useEffect on mount
  const [sessionToken, setSessionToken] = useState(() => crypto.randomUUID());
  const dropdownRef = useRef(null);

  const { data, isLoading } = useGoongAutocomplete(value, sessionToken);
  const predictions = data?.predictions || [];

  // Handle click outside to close dropdown
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelect = async (prediction) => {
    setIsOpen(false);
    onChange(prediction.description);

    try {
      // Get detail to fetch lat/lng
      const detail = await goongApi.placeDetail(
        prediction.place_id,
        sessionToken,
      );
      if (detail?.result?.geometry?.location) {
        const { lat, lng } = detail.result.geometry.location;
        onSelectPlace(prediction.description, lat, lng);
      }
    } catch (error) {
      console.error("Error fetching place detail:", error);
    }

    // Refresh session token for next search
    setSessionToken(crypto.randomUUID());
  };

  return (
    <div className="space-y-1.5 relative w-full" ref={dropdownRef}>
      <label className="text-sm font-semibold text-gray-700 dark:text-gray-300 ml-1">
        Địa chỉ cụ thể <span className="text-red-500">*</span>
      </label>
      <div className="relative">
        <div className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400">
          <Search size={18} />
        </div>
        <input
          type="text"
          value={value}
          onChange={(e) => {
            onChange(e.target.value);
            setIsOpen(true);
          }}
          onFocus={() => setIsOpen(true)}
          placeholder="Tìm quán cafe, tên đường, khu vực..."
          className={`w-full bg-gray-50 dark:bg-gray-900 border ${error ? "border-red-500 focus:border-red-500 focus:ring-red-500/20" : "border-gray-200 dark:border-gray-700 focus:border-blue-500 focus:ring-blue-500/20"} rounded-xl pl-11 pr-4 py-3 text-gray-900 dark:text-white focus:ring-2 transition-all outline-none`}
        />
        {isLoading && isOpen && (
          <div className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400">
            <Loader2 size={16} className="animate-spin" />
          </div>
        )}
      </div>

      {isOpen && predictions.length > 0 && (
        <div className="absolute top-full left-0 right-0 mt-2 bg-white dark:bg-gray-800 rounded-2xl shadow-xl shadow-blue-900/10 border border-gray-100 dark:border-gray-700 overflow-hidden z-50">
          <ul className="max-h-[300px] overflow-y-auto">
            {predictions.map((prediction) => (
              <li key={prediction.place_id}>
                <button
                  type="button"
                  onClick={() => handleSelect(prediction)}
                  className="w-full text-left px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-start gap-3 border-b border-gray-50 dark:border-gray-800 last:border-0"
                >
                  <MapPin size={18} className="text-gray-400 mt-0.5 shrink-0" />
                  <div>
                    <h4 className="font-semibold text-gray-900 dark:text-white text-sm">
                      {prediction.structured_formatting?.main_text ||
                        prediction.description}
                    </h4>
                    {prediction.structured_formatting?.secondary_text && (
                      <p className="text-sm text-gray-500 line-clamp-1 mt-0.5">
                        {prediction.structured_formatting.secondary_text}
                      </p>
                    )}
                  </div>
                </button>
              </li>
            ))}
          </ul>
          <div className="p-2.5 bg-gray-50 dark:bg-gray-900/50 text-xs text-center text-gray-500 flex items-center justify-center gap-2">
            <Navigation size={12} />
            Được cung cấp bởi Goong Maps
          </div>
        </div>
      )}
    </div>
  );
};

export default AddressAutocomplete;
