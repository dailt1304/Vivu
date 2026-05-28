import React, {
  useState,
  useEffect,
  useCallback,
  useMemo,
  Suspense,
} from "react";
import { useParams, useNavigate } from "react-router-dom";
import { format, parseISO, differenceInDays } from "date-fns";
import {
  ArrowLeft,
  Calendar,
  MapPin,
  Users,
  Clock,
  Navigation,
  Loader2,
  Copy,
} from "lucide-react";

import AppNavbar from "../../../components/layout/AppNavbar";
import MapContainer from "../../../components/common/map/MapContainer";
import ConnectionError from "../../../components/common/ConnectionError";
import tripApi from "../../../api/tripApi";
import toast from "../../../utils/toast";

// Vercel Rule: bundle-dynamic-imports
const DestinationDrawer = React.lazy(
  () => import("../../../components/common/drawers/DestinationDrawer"),
);

// Vercel Rule: rendering-hoist-jsx
const TRANSPORT_ICONS = {
  walking: { emoji: "🚶", label: "Đi bộ" },
  car: { emoji: "🚗", label: "Ô tô" },
  taxi: { emoji: "🚕", label: "Taxi" },
  bus: { emoji: "🚌", label: "Xe buýt" },
  motorbike: { emoji: "🏍️", label: "Xe máy" },
  motorcycle: { emoji: "🏍️", label: "Xe máy" },
  bicycle: { emoji: "🚲", label: "Xe đạp" },
  train: { emoji: "🚆", label: "Tàu hỏa" },
  flight: { emoji: "✈️", label: "Máy bay" },
  default: { emoji: "📍", label: "Di chuyển" },
};

const getTransportIcon = (mode) => {
  if (!mode) return TRANSPORT_ICONS.default;
  const key = mode.toLowerCase();
  return TRANSPORT_ICONS[key] || TRANSPORT_ICONS.default;
};

const parseImages = (imagesData) => {
  try {
    let parsed = [];
    if (typeof imagesData === "string" && imagesData.startsWith("[")) {
      parsed = JSON.parse(imagesData);
    } else if (Array.isArray(imagesData)) {
      parsed = imagesData;
    }
    return parsed.map((img) => {
      if (!img) return null;
      if (typeof img === "string") return img;
      return img?.url || img?.imageUrl || img?.src || null;
    }).filter(Boolean);
  } catch (e) {
    console.error("Failed to parse images:", e);
    return [];
  }
};

const formatTimeSpan = (timeStr) => {
  if (!timeStr) return "--:--";
  return timeStr.substring(0, 5);
};

const calculateDistance = (lat1, lon1, lat2, lon2) => {
  if (!lat1 || !lon1 || !lat2 || !lon2) return null;
  const R = 6371; // km
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c; // in km
};

const formatDistance = (dist) => {
  if (dist === null) return "";
  if (dist < 1) return `${Math.round(dist * 1000)}m`;
  return `${dist.toFixed(1)} km`;
};

const PublicTripDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  // Data states
  const [tripData, setTripData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isCopying, setIsCopying] = useState(false);

  // UI states
  const [selectedDayIndex, setSelectedDayIndex] = useState(0);
  const [flyToLocation, setFlyToLocation] = useState(null);
  const [hoveredItemId, setHoveredItemId] = useState(null);

  // Drawer state
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerData, setDrawerData] = useState(null);

  // Fetch public trip data
  const fetchTrip = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const response = await tripApi.getPublicById(id);
      if (response.success && response.data) {
        setTripData(response.data);
      } else {
        setError("Không tìm thấy chuyến đi");
      }
    } catch {
      setError("Không thể tải thông tin chuyến đi");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchTrip();
  }, [fetchTrip]);

  // Derived data
  const tripDays = useMemo(
    () => (tripData?.tripDays || []).filter((d) => d.dayIndex !== 0),
    [tripData],
  );

  const currentDay = tripDays[selectedDayIndex] || null;

  const startDate = tripData?.startDate ? parseISO(tripData.startDate) : null;
  const endDate = tripData?.endDate ? parseISO(tripData.endDate) : null;
  const totalDays =
    startDate && endDate ? differenceInDays(endDate, startDate) : 0;

  // Map locations - only show locations from the selected day
  // Vercel Rule: rendering-hoist-jsx - category field enables MapContainer to render category-specific icons
  const currentDayMapLocations = useMemo(() => {
    const locations = currentDay?.locations || [];
    return locations.map((loc) => ({
      id: loc.location?.id || loc.id,
      title: loc.location?.name || "Địa điểm",
      latitude: loc.location?.latitude || 16.06,
      longitude: loc.location?.longitude || 108.22,
      category: loc.location?.category?.name || null,
      categoryIcon: loc.location?.category?.iconUrl,
    }));

  }, [currentDay]);

  // Vercel Rule: rendering-conditional-render - Stable references via useCallback
  const handleOpenLocation = useCallback((locData) => {
    setDrawerData(locData);
    setDrawerOpen(true);
  }, []);

  const handleMarkerClick = useCallback(
    (markerLocation) => {
      // The marker gives us basic location info. Check if we have the full location in trip data to open its details
      for (const day of tripDays) {
        for (const loc of day.locations || []) {
          if ((loc.location?.id || loc.id) === markerLocation.id) {
            setDrawerData(loc.location);
            setDrawerOpen(true);
            return;
          }
        }
      }
    },
    [tripDays],
  );

  const handleFlyTo = useCallback((loc) => {
    if (loc.location?.latitude && loc.location?.longitude) {
      setFlyToLocation({
        latitude: loc.location.latitude,
        longitude: loc.location.longitude,
      });
    }
  }, []);

  // Vercel Rule: rendering-hoist-jsx - Stable onClose reference
  const closeDrawer = useCallback(() => setDrawerOpen(false), []);

  const handleCopyTrip = useCallback(async () => {
    if (!id) return;
    try {
      setIsCopying(true);
      const response = await tripApi.copy(id);
      if (response.success) {
        toast.success("Đã sao chép chuyến đi thành công!");
      } else {
        toast.error(response.message || "Không thể sao chép chuyến đi");
      }
    } catch {
      toast.error("Lỗi khi sao chép chuyến đi");
    } finally {
      setIsCopying(false);
    }
  }, [id]);

  // Loading state
  // Vercel Rule: js-early-exit
  if (loading) {
    return (
      <div className="h-screen flex flex-col bg-white">
        <AppNavbar />
        <div className="flex-1 flex items-center justify-center">
          <div className="flex items-center gap-3 text-blue-600">
            <Loader2 className="animate-spin" size={24} />
            <span className="font-medium">Đang tải chuyến đi...</span>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  // Vercel Rule: js-early-exit
  if (error || !tripData) {
    return (
      <div className="h-screen flex flex-col bg-white">
        <AppNavbar />
        <ConnectionError
          message={error || "Lỗi dữ liệu"}
          onRetry={fetchTrip}
          isRetrying={loading}
        />
      </div>
    );
  }

  const ownerName = tripData.owner?.fullName || "Ẩn danh";
  const ownerAvatar = tripData.owner?.avatarUrl;

  return (
    <div className="h-screen flex flex-col bg-gray-50 overflow-hidden">
      <AppNavbar />

      <div className="flex-1 flex overflow-hidden">
        {/* ==================== LEFT PANEL ==================== */}
        <div className="w-full lg:w-1/2 overflow-y-auto">
          {/* Back Button & Actions */}
          <div className="sticky top-0 z-20 bg-white/80 backdrop-blur-md border-b border-gray-100 px-4 py-3 flex justify-between items-center">
            <button
              onClick={() => navigate(-1)}
              className="flex items-center gap-2 text-gray-600 hover:text-gray-900 font-medium transition-colors text-sm cursor-pointer"
            >
              <ArrowLeft size={18} />
              Quay lại
            </button>
            <button
              onClick={handleCopyTrip}
              disabled={isCopying}
              className="flex items-center gap-1.5 bg-blue-50 text-blue-600 hover:bg-blue-100 px-3 py-1.5 rounded-lg text-sm font-semibold transition-colors disabled:opacity-50 cursor-pointer"
            >
              {isCopying ? (
                <Loader2 size={16} className="animate-spin" />
              ) : (
                <Copy size={16} />
              )}
              Sao chép
            </button>
          </div>

          {/* Cover Image */}
          {tripData.coverUrl && (
            <div className="relative h-56 sm:h-64">
              <img
                src={tripData.coverUrl}
                alt={tripData.title}
                className="w-full h-full object-cover"
              />
              <div className="absolute inset-0 bg-linear-to-t from-black/60 via-transparent to-transparent" />
            </div>
          )}

          {/* Trip Info Header */}
          <div className="px-6 py-6 bg-white border-b border-gray-100">
            <h1 className="text-2xl sm:text-3xl font-extrabold text-gray-900 mb-3 leading-tight">
              {tripData.title || "Chuyến đi"}
            </h1>

            {tripData.description && (
              <p className="text-gray-600 text-sm leading-relaxed mb-5 line-clamp-3">
                {tripData.description}
              </p>
            )}

            {/* Owner */}
            <div className="flex items-center gap-3 mb-5">
              {ownerAvatar ? (
                <img
                  src={ownerAvatar}
                  alt={ownerName}
                  className="w-9 h-9 rounded-full object-cover ring-2 ring-blue-100"
                />
              ) : (
                <div className="w-9 h-9 rounded-full bg-linear-to-br from-blue-400 to-indigo-500 flex items-center justify-center text-white text-sm font-bold ring-2 ring-blue-100">
                  {ownerName.charAt(0)}
                </div>
              )}
              <div>
                <p className="text-sm font-semibold text-gray-900">
                  {ownerName}
                </p>
                <p className="text-xs text-gray-400">Người tạo</p>
              </div>
            </div>

            {/* Stats Badges */}
            <div className="flex flex-wrap gap-2">
              {startDate && endDate && (
                <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-blue-50 text-blue-700 text-xs font-semibold">
                  <Calendar size={14} />
                  {format(startDate, "dd/MM")} - {format(endDate, "dd/MM/yyyy")}
                </span>
              )}
              <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-emerald-50 text-emerald-700 text-xs font-semibold">
                <Clock size={14} />
                {totalDays} ngày
              </span>
              <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-violet-50 text-violet-700 text-xs font-semibold">
                <MapPin size={14} />
                {tripData.locationCount || 0} địa điểm
              </span>
              <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-amber-50 text-amber-700 text-xs font-semibold">
                <Users size={14} />
                {tripData.memberCount || 0} thành viên
              </span>
            </div>
          </div>

          {/* ==================== DAY TABS ==================== */}
          <div className="sticky top-[52px] z-10 bg-white border-b border-gray-100 px-4">
            <div className="flex gap-1 overflow-x-auto hide-scrollbar py-2">
              {tripDays.map((day, idx) => {
                const isActive = idx === selectedDayIndex;
                const dayDate = day.dayDate ? parseISO(day.dayDate) : null;
                return (
                  <button
                    key={day.id || idx}
                    onClick={() => setSelectedDayIndex(idx)}
                    className={`shrink-0 px-4 py-2.5 rounded-xl text-sm font-semibold transition-all duration-200 cursor-pointer ${
                      isActive
                        ? "bg-gradient-primary text-white shadow-md shadow-blue-200"
                        : "bg-gray-50 text-gray-600 hover:bg-gray-100"
                    }`}
                  >
                    <div>Ngày {day.dayIndex || idx + 1}</div>
                    {dayDate && (
                      <div
                        className={`text-[10px] mt-0.5 ${isActive ? "text-white/80" : "text-gray-400"}`}
                      >
                        {format(dayDate, "dd/MM")}
                      </div>
                    )}
                  </button>
                );
              })}
            </div>
          </div>

          {/* ==================== ITINERARY TIMELINE ==================== */}
          <div className="p-8 md:p-12 max-w-3xl mx-auto">
            {/* Itinerary Section */}
            <div className="pt-2">
              <div className="flex flex-col gap-10">
                {/* Vercel Rule: rendering-conditional-render */}
                {currentDay ? (
                  <div className="relative">
                    {/* Day Header */}
                    <div className="flex items-center gap-4 mb-6 relative">
                      <div className="w-12 h-12 rounded-full bg-gradient-primary flex items-center justify-center text-white font-bold text-xl shadow-lg shrink-0 z-9">
                        {currentDay.dayIndex || selectedDayIndex + 1}
                      </div>
                      <div>
                        <h4 className="font-bold text-xl text-gray-900">
                          {currentDay.title ||
                            `Ngày ${currentDay.dayIndex || selectedDayIndex + 1}`}
                        </h4>
                        {currentDay.dayDate && (
                          <p className="text-sm text-gray-500 mt-1">
                            {format(parseISO(currentDay.dayDate), "dd/MM/yyyy")}
                          </p>
                        )}
                      </div>
                    </div>

                    {/* Locations Timeline */}
                    <div className="ml-6 border-l-2 border-dashed border-gray-200 pl-8 space-y-6 pb-4">
                      {(currentDay.locations || []).length > 0 ? (
                        currentDay.locations.map((loc, idx) => {
                          const transportConfig = getTransportIcon(
                            loc.transportMode,
                          );
                          const locationData = loc.location || {};

                          let distanceText = null;
                          if (idx > 0) {
                            const prevLoc = currentDay.locations[idx - 1];
                            const prevCoords = prevLoc.location || {};
                            const currCoords = loc.location || {};
                            const dist = calculateDistance(
                              prevCoords.latitude,
                              prevCoords.longitude,
                              currCoords.latitude,
                              currCoords.longitude,
                            );
                            if (dist !== null) {
                              distanceText = formatDistance(dist);
                            }
                          }

                          return (
                            <div key={loc.id} className="relative">
                              {/* Transport Connector */}
                              {idx > 0 && (
                                <div className="absolute -top-10 -left-[45px] py-1 bg-white">
                                  <div className="px-2 py-0.5 border border-gray-200 rounded-full font-medium text-xs text-gray-500 flex items-center gap-1.5 bg-gray-50/80 backdrop-blur-sm">
                                    {loc.transportMode && transportConfig ? (
                                      <span
                                        className="flex items-center gap-1"
                                        title={transportConfig.label}
                                      >
                                        <span className="text-sm">
                                          {transportConfig.emoji}
                                        </span>{" "}
                                        {transportConfig.label}
                                      </span>
                                    ) : null}
                                    {distanceText && (
                                      <span
                                        className={`px-1.5 py-0.5 items-center justify-center flex rounded text-[10px] font-bold ${!loc.transportMode ? "bg-gray-100 text-gray-600" : "bg-blue-100/50 text-blue-600"}`}
                                      >
                                        {distanceText}
                                      </span>
                                    )}
                                  </div>
                                </div>
                              )}

                              <div
                                className="relative bg-white border border-gray-100 p-5 rounded-2xl shadow-sm hover:shadow-md transition-shadow cursor-pointer"
                                onMouseEnter={() => {
                                  setHoveredItemId(locationData.id || loc.id);
                                  handleFlyTo(loc);
                                }}
                                onMouseLeave={() => setHoveredItemId(null)}
                                onClick={() => handleOpenLocation(locationData)}
                              >
                                {/* Timeline dot */}
                                <div className="absolute -left-[41px] top-6 w-5 h-5 rounded-full bg-white border-4 border-blue-500"></div>

                                <div className="flex items-center gap-3 mb-3">
                                  <div className="px-2.5 py-1 bg-gray-100 rounded-lg text-xs font-bold text-gray-600 flex items-center gap-1.5">
                                    <Clock size={12} />
                                    {formatTimeSpan(loc.startTime)}
                                    {loc.endTime
                                      ? ` - ${formatTimeSpan(loc.endTime)}`
                                      : ""}
                                  </div>
                                </div>

                                <div className="flex flex-col sm:flex-row items-start justify-between gap-4">
                                  <div className="flex-1">
                                    <div className="flex flex-col md:flex-row gap-4">
                                      {/* Image placeholder for location */}
                                      <div className="w-full md:w-32 h-32 shrink-0 rounded-xl overflow-hidden bg-gray-100 border border-gray-100 shadow-inner">
                                        {(() => {
                                          const parsedImgs = parseImages(
                                            locationData.locationDetail?.images,
                                          );
                                          const imgSrc =
                                            parsedImgs[0] ||
                                            locationData.imageUrl ||
                                            "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?q=80&w=400&auto=format&fit=crop";
                                          return (
                                            <img
                                              src={imgSrc}
                                              alt={locationData.name}
                                              className="w-full h-full object-cover transition-transform duration-500 hover:scale-110"
                                            />
                                          );
                                        })()}
                                      </div>

                                      <div className="flex-1">
                                        <h5 className="font-bold text-lg text-gray-900 mb-1 group-hover:text-blue-600 transition-colors">
                                          {locationData.name || "Địa điểm"}
                                        </h5>
                                        {locationData.address && (
                                          <div className="flex items-center gap-1 text-sm text-gray-500 mb-3">
                                            <MapPin
                                              size={14}
                                              className="text-blue-500 shrink-0"
                                            />
                                            <span className="line-clamp-1">
                                              {locationData.address}
                                            </span>
                                          </div>
                                        )}

                                        {loc.note && (
                                          <p className="text-sm text-gray-600 bg-blue-50/50 p-3 rounded-xl border border-blue-100/50 mt-2">
                                            {loc.note}
                                          </p>
                                        )}
                                      </div>
                                    </div>
                                  </div>

                                  <button
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleOpenLocation(locationData);
                                    }}
                                    className="sm:mt-1 shrink-0 px-4 py-2 text-sm font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 rounded-xl transition-colors cursor-pointer w-full sm:w-auto"
                                  >
                                    Xem chi tiết
                                  </button>
                                </div>
                              </div>
                            </div>
                          );
                        })
                      ) : (
                        <div className="text-sm text-gray-400 py-4 italic">
                          Chưa có lịch trình cho ngày này.
                        </div>
                      )}
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-12 bg-gray-50 rounded-2xl border border-dashed border-gray-200">
                    <p className="text-gray-500 font-medium">
                      Chuyến đi này hiện chưa có lịch trình nào.
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* ==================== RIGHT PANEL - MAP ==================== */}
        <div className="hidden lg:block lg:w-1/2 relative bg-gray-100">
          <MapContainer
            locations={currentDayMapLocations}
            onMarkerClick={handleMarkerClick}
            flyToLocation={flyToLocation}
            hoveredItemId={hoveredItemId}
          />

          {/* Map Legend */}
          <div className="absolute bottom-6 left-6 bg-white/90 backdrop-blur-md rounded-2xl shadow-lg px-4 py-3 text-xs text-gray-600 border border-white/50">
            <div className="flex items-center gap-2 font-semibold text-gray-800 mb-1">
              <Navigation size={14} className="text-blue-500" />
              Lịch trình Ngày {currentDay?.dayIndex || selectedDayIndex + 1}
            </div>
            <p className="text-gray-400">
              {currentDayMapLocations.length} địa điểm trên bản đồ
            </p>
          </div>
        </div>
      </div>

      {/* ==================== DESTINATION DRAWER ==================== */}
      <Suspense fallback={null}>
        {drawerOpen && (
          <DestinationDrawer
            isOpen={drawerOpen}
            onClose={closeDrawer}
            data={drawerData}
          />
        )}
      </Suspense>
    </div>
  );
};

export default PublicTripDetailPage;
