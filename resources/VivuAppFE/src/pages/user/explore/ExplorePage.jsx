import React, {
  useState,
  useEffect,
  useMemo,
  Suspense,
  useRef,
  useCallback,
} from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import AppNavbar from "../../../components/layout/AppNavbar";
import LocationCard from "../../../components/user/explore/LocationCard";
import DestinationDrawer from "../../../components/common/drawers/DestinationDrawer";
const MapContainer = React.lazy(
  () => import("../../../components/common/map/MapContainer"),
);
import CategoryIcon from "../../../components/common/CategoryIcon";

import {
  Search,
  SlidersHorizontal,
  MapPin,
  ChevronDown,
  Navigation,
  Navigation2,
  Plus,
  Sparkles,
  TreePine,
  Umbrella,
  Building2,
  UtensilsCrossed,
  Coffee,
  Ticket,
  Landmark,
  ShoppingBag,
  List,
  Map as MapIcon,
  ChevronsLeft,
  ChevronsRight,
  Filter,
  CheckCircle2,
  X,
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import {
  useLocationsByFilter,
  useLocationCategories,
  useNearbyLocations,
} from "../../../hooks/locations/useLocations";
import { useSearchCities } from "../../../hooks/cities/useCities";
import { useUserCity } from "../../../hooks/maps/useUserCity";
import useGeolocation from "../../../hooks/maps/useGeolocation";
import { useAuth } from "../../../contexts/auth-context";
import toast from "../../../utils/toast";
import { useExploreFilters } from "../../../hooks/locations/useExploreFilters";
import { useActivePlanningTrip } from "../../../hooks/trips/useActivePlanningTrip";
import tripLocationApi from "../../../api/tripLocationApi";

const SubmitLocationModal = React.lazy(
  () => import("../../../components/user/locations/SubmitLocationModal"),
);
const AddToCollectionModal = React.lazy(
  () => import("../../../components/user/saved/AddToCollectionModal"),
);

// Advanced Shimmer Skeleton for Horizontal Layout
const Shimmer = React.memo(() => (
  <div
    className="absolute inset-0 z-50 bg-linear-to-r from-transparent via-white/50 to-transparent pointer-events-none w-[200%]"
    style={{
      animation: "shimmer 1.5s linear infinite",
    }}
  />
));

const LocationSkeleton = React.memo(() => (
  <div className="bg-white/80 backdrop-blur-md rounded-2xl overflow-hidden ring-1 ring-slate-200 shadow-sm relative flex flex-row h-36">
    <Shimmer />
    <div className="w-[120px] sm:w-[140px] h-full bg-slate-200/50 shrink-0" />
    <div className="p-4 space-y-3 flex-1 flex flex-col justify-between">
      <div>
        <div className="flex justify-between items-start mb-2">
          <div className="h-5 bg-slate-300/80 rounded w-2/3" />
          <div className="h-4 bg-slate-300/80 rounded w-10 shrink-0 ml-2" />
        </div>
        <div className="h-4 bg-slate-200/80 rounded w-1/2" />
      </div>
      <div className="pt-2 flex items-center gap-2">
        <div className="h-5 bg-slate-200/80 rounded-full w-20" />
      </div>
    </div>
  </div>
));
LocationSkeleton.displayName = "LocationSkeleton";

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

// Removed CATEGORY_ICONS as it's now handled by the CategoryIcon component

const CARD_INITIAL = { opacity: 0, y: 15 };
const CARD_ANIMATE = { opacity: 1, y: 0 };
const getCardTransition = (i) => ({
  duration: 0.3,
  delay: i * 0.03,
  ease: "easeOut",
});

const HorizontalLocationCard = React.memo((props) => (
  <LocationCard {...props} layout="horizontal" />
));

const ExplorePage = () => {
  const [searchParams] = useSearchParams();
  const pickAlternativeMode = searchParams.get("mode") === "pick-alternative";
  const tripLocationId = searchParams.get("tripLocationId");
  const tripId = searchParams.get("tripId");
  const refLoc = searchParams.get("refLoc");
  const refLat = searchParams.get("lat")
    ? parseFloat(searchParams.get("lat"))
    : null;
  const refLng = searchParams.get("lng")
    ? parseFloat(searchParams.get("lng"))
    : null;

  const {
    filters,
    cityName,
    activeSortOption,
    currentPage,
    setCity,
    setCategory,
    setSort,
    setPage,
    setSearchText,
  } = useExploreFilters();

  // Local input state for the search box (immediate UI feedback)
  const [searchInput, setSearchInput] = useState(filters.searchText || "");
  const searchTimerRef = useRef(null);
  const { trigger: searchCities, data: searchResults } = useSearchCities();

  const [isSubmitAltLoading, setIsSubmitAltLoading] = useState(false);
  const [confirmAltLocation, setConfirmAltLocation] = useState(null);

  const [isCitySelectorOpen, setIsCitySelectorOpen] = useState(false);
  const [citySearch, setCitySearch] = useState(cityName || "");
  const [selectedDestination, setSelectedDestination] = useState(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isSubmitOpen, setIsSubmitOpen] = useState(false);
  const [collectionModalState, setCollectionModalState] = useState({
    isOpen: false,
    locationId: null,
  });
  const [isSortOpen, setIsSortOpen] = useState(false);
  const [isFilterOpen, setIsFilterOpen] = useState(false);
  const mapRef = useRef(null);
  const hoveredItemIdRef = useRef(null);
  const [flyToLocation, setFlyToLocation] = useState(null);
  const [showMapOnMobile, setShowMapOnMobile] = useState(false);

  const { isAuthenticated, userId } = useAuth();
  const navigate = useNavigate();

  // Pick-alternative mode: use primary location's coordinates for nearby search
  const referencePosition = useMemo(() => {
    if (pickAlternativeMode && refLat && refLng) {
      return { latitude: refLat, longitude: refLng };
    }
    return null;
  }, [pickAlternativeMode, refLat, refLng]);

  // Auto-init for pick-alternative mode: fly to location + switch to nearby
  const pickAltInitRef = useRef(false);
  useEffect(() => {
    if (pickAlternativeMode && referencePosition && !pickAltInitRef.current) {
      pickAltInitRef.current = true;
      // Fly map to the primary location
      setFlyToLocation({
        latitude: referencePosition.latitude,
        longitude: referencePosition.longitude,
        zoom: 14,
      });
      // Auto-switch to nearby sort mode
      if (activeSortOption !== "nearby") {
        setSort("nearby", true);
      }
    }
  }, [pickAlternativeMode, referencePosition, activeSortOption, setSort]);

  const useFilterApi = activeSortOption !== "nearby";

  const { data: locationsData, isLoading: isFilterLoading } =
    useLocationsByFilter(isAuthenticated && useFilterApi ? filters : null);
  const { data: categoriesData } = useLocationCategories();

  const { position, isLoading: geoLoading } = useGeolocation();

  // In pick-alternative mode, use the reference location for nearby;
  // otherwise fall back to the user's geolocation
  const nearbyPosition = referencePosition || position;
  const { data: nearbyData, isLoading: nearbyLoading } = useNearbyLocations(
    nearbyPosition,
    20000, // Increased radius to find more results
    80, // Fetch 80 for pagination (4 pages of 20)
    filters.categoryId,
  );

  const isNearbyMode = activeSortOption === "nearby";

  const isLocationsLoading = isNearbyMode
    ? nearbyLoading || (referencePosition ? false : geoLoading)
    : isFilterLoading;

  const fetchedLocations = useMemo(() => {
    // Shared mapping logic...
    const mapItem = (item) => ({
      id: item.id,
      title: item.name,
      location: item.address || item.cityName || item.city?.name || "Vietnam",
      cityName: item.cityName || item.city?.name || "Vietnam",
      image:
        parseImages(item.locationDetail?.images)[0]?.url ||
        parseImages(item.locationDetail?.images)[0] ||
        item.images?.[0]?.url ||
        item.images?.[0] ||
        item.thumbnailUrl ||
        null,
      rating: item.ratingAverage || 0,
      reviews: item.ratingCount || 0,
      category: item.categoryName || item.category?.name || "General",
      categoryId: item.categoryId || item.category?.id,
      categoryIcon:
        item.category?.iconUrl || item.categoryIconUrl || item.categoryIcon,
      price: item.locationDetail?.currentPrice
        ? `${item.locationDetail.currentPrice.toLocaleString()}₫`
        : "Miễn phí",
      latitude: item.latitude,
      longitude: item.longitude,
      description: item.description || "",
      distanceInMeters: pickAlternativeMode ? null : item.distanceInMeters,
      isVerified: item.isVerified,
    });

    if (isNearbyMode && nearbyData) return nearbyData.map(mapItem);
    if (locationsData?.items) return locationsData.items.map(mapItem);
    return [];
  }, [
    locationsData,
    nearbyData,
    activeSortOption,
    isNearbyMode,
    pickAlternativeMode,
  ]);

  const categoriesList = useMemo(() => {
    const list = categoriesData?.items || [];
    return [{ id: null, name: "All" }, ...list];
  }, [categoriesData]);

  const handleCategorySelect = (catId) => {
    setCategory(catId);
  };

  const { data: ipCity, mutate: refetchCity } = useUserCity();
  const { planningTrip, isLoading: planningTripLoading } = useActivePlanningTrip(userId);
  const [isInitCityDone, setIsInitCityDone] = useState(false);

  useEffect(() => {
    // If the city was already explicitly set (like via URL query), we're done
    if (filters.cityId) {
      setIsInitCityDone(true);
      return;
    }
    
    // Wait for the planning trip hook to complete (either data or null)
    if (planningTripLoading) return;

    const resolveCityFromText = async (cityNameText) => {
      if (!cityNameText) return false;
      try {
        const res = await searchCities({
          searchText: cityNameText,
          pageSize: 1,
        });
        if (res?.items && res.items.length > 0) {
          const city = res.items[0];
          setCity(city.id, city.name);
          setCitySearch(city.name);
          if (city.latitude && city.longitude) {
            setFlyToLocation({
              latitude: city.latitude,
              longitude: city.longitude,
              zoom: 12,
            });
          }
          return true;
        }
      } catch (error) {
        console.error("Failed to auto-resolve city ID:", error);
      }
      return false;
    };

    const initCity = async () => {
      // Priority 1: Check if user has an active planning trip with a valid city
      if (planningTrip?.cityId && planningTrip?.cityName) {
        setCity(planningTrip.cityId, planningTrip.cityName);
        setCitySearch(planningTrip.cityName);
        setIsInitCityDone(true);
        return;
      } 
      
      // Priority 2: Use trip's cityName or title as a search term
      if (planningTrip?.cityName) {
        const found = await resolveCityFromText(planningTrip.cityName);
        if (found) {
          setIsInitCityDone(true);
          return;
        }
      }
      
      if (planningTrip?.title) {
        const found = await resolveCityFromText(planningTrip.title);
        if (found) {
          setIsInitCityDone(true);
          return;
        }
      }

      // Priority 3: Fallback to IP-based city
      if (ipCity?.city) {
        let normalizedCity = ipCity.city
          .replace(/^(Thành phố|Tỉnh)\s+/i, "")
          .replace(/\s+(City|Province)$/i, "")
          .trim();

        const found = await resolveCityFromText(normalizedCity);
        if (found) {
          setIsInitCityDone(true);
          return;
        }
      }

      // If everything fails, mark as done so UI doesn't spin forever
      setIsInitCityDone(true);
    };

    void initCity();
  }, [ipCity, searchCities, filters.cityId, setCity, planningTrip, planningTripLoading]);

  const handleManualLocationClick = () => {
    setCity(null, "");
    setCitySearch("");
    refetchCity();
    setIsCitySelectorOpen(false);
  };

  // Debounced search: update URL param after 400ms of inactivity
  const handleSearchInputChange = useCallback(
    (e) => {
      const val = e.target.value;
      setSearchInput(val);
      if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
      searchTimerRef.current = setTimeout(() => {
        setSearchText(val);
      }, 400);
    },
    [setSearchText],
  );

  // No client-side filtering needed — server handles searchText
  const finalLocations = useMemo(() => {
    if (pickAlternativeMode && refLoc) {
      return fetchedLocations.filter(
        (loc) => loc.title?.toLowerCase() !== refLoc.toLowerCase()
      );
    }
    return fetchedLocations;
  }, [fetchedLocations, pickAlternativeMode, refLoc]);

  const scrollContainerRef = useRef(null);

  useEffect(() => {
    // Sync scroll with page changes...
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollTo({ top: 0, behavior: "smooth" });
    }
  }, [currentPage]);

  const handlePickAlternative = async (loc) => {
    if (!tripLocationId || !tripId) return;
    setIsSubmitAltLoading(true);
    try {
      const res = await tripLocationApi.addAlternative(tripLocationId, {
        locationId: loc.id,
        priority: 1, // Default priority, or logic to calculate
        reason: "Chọn dự phòng từ bản đồ",
      });
      if (res.isSuccess || res.success) {
        toast.success(`Đã thêm ${loc.title} làm địa điểm dự phòng`);
        // Navigate forward to trip page (not back) to ensure fresh mount
        // replace: true prevents returning to pick-alternative screen
        navigate(`/trips/${tripId}`, { replace: true, state: { broadcastNeeded: true } });
      } else {
        toast.error(res.message || "Không thể thêm địa điểm dự phòng.");
      }
    } catch (error) {
      console.error(error);
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        "Lỗi khi thêm địa điểm dự phòng";
      toast.error(errorMessage);
    } finally {
      setIsSubmitAltLoading(false);
      setConfirmAltLocation(null);
    }
  };

  const handleMapMarkerClick = useCallback(
    (loc) => {
      // Normal flow, let the Drawer open to show details first
      setSelectedDestination({
        ...loc,
        name: loc.title,
        img: loc.image,
        province: loc.location,
      });
      setIsDrawerOpen(true);
    },
    [tripLocationId, tripId, navigate],
  );

  const handleCardMouseEnter = useCallback((id) => {
    hoveredItemIdRef.current = id;
    mapRef.current?.setHovered(id);
  }, []);

  const handleCardMouseLeave = useCallback(() => {
    hoveredItemIdRef.current = null;
    mapRef.current?.setHovered(null);
  }, []);

  const handleCardClick = useCallback(
    (loc) => {
      // Normal flow, let the Drawer open to show details first
      setSelectedDestination({
        ...loc,
        name: loc.title,
        img: loc.image,
        province: loc.location,
      });
      setIsDrawerOpen(true);
    },
    [tripLocationId, tripId, navigate, refLoc],
  );

  const handleAddToCollection = useCallback((locationId) => {
    setCollectionModalState({ isOpen: true, locationId });
  }, []);

  const paginatedLocations = useMemo(() => {
    if (activeSortOption === "nearby") {
      return finalLocations.slice((currentPage - 1) * 20, currentPage * 20);
    }
    return finalLocations; // Server-side pagination already handled via PAGE_SIZE
  }, [finalLocations, currentPage, activeSortOption]);

  const totalPages = useMemo(() => {
    if (activeSortOption === "nearby") {
      return Math.ceil(finalLocations.length / 20);
    }
    return locationsData?.totalPages || 1;
  }, [finalLocations, locationsData, activeSortOption]);

  const totalCount = useMemo(() => {
    if (activeSortOption === "nearby") {
      return finalLocations.length;
    }
    return locationsData?.totalCount !== undefined
      ? locationsData?.totalCount
      : finalLocations.length;
  }, [finalLocations, locationsData, activeSortOption]);

  const visiblePages = useMemo(() => {
    if (totalPages <= 7)
      return Array.from({ length: totalPages }, (_, i) => i + 1);

    const pages = [];
    const leftLimit = Math.max(2, currentPage - 1);
    const rightLimit = Math.min(totalPages - 1, currentPage + 1);

    pages.push(1);

    if (leftLimit > 2) pages.push("...");

    for (let i = leftLimit; i <= rightLimit; i++) {
      pages.push(i);
    }

    if (rightLimit < totalPages - 1) pages.push("...");

    pages.push(totalPages);

    return pages;
  }, [totalPages, currentPage]);

  if (!isAuthenticated) {
    return (
      <div className="flex flex-col h-dvh overflow-hidden bg-white pb-16 lg:pb-0">
        <AppNavbar />
        <div className="flex flex-1 items-center justify-center p-4">
          {/* Fallback login banner ... same as before*/}
          <div className="w-full max-w-md p-8 text-center bg-white rounded-3xl border border-slate-100 shadow-xl">
            <MapPin size={48} className="text-blue-500 mx-auto mb-6" />
            <h2 className="text-2xl font-bold text-slate-900 mb-4">
              Khám phá trải nghiệm
            </h2>
            <button
              onClick={() => navigate("/login")}
              className="w-full py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-lg hover:scale-105 active:scale-95 transition-all"
            >
              Đăng nhập ngay
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-dvh overflow-hidden bg-slate-100 pb-16 lg:pb-0 relative">
      <AppNavbar />

      {pickAlternativeMode && (
        <div className="sticky top-0 z-40 bg-amber-50 border-b border-amber-200 px-4 py-3 flex items-center justify-between shadow-sm">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-amber-100 text-amber-700 rounded-lg">
              <MapIcon size={18} />
            </div>
            <div>
              <p className="text-sm font-bold text-amber-900">
                Chế độ chọn điểm dự phòng
              </p>
              <p className="text-xs text-amber-700 font-medium">
                Cho: {refLoc || "địa điểm hiện tại"}
              </p>
            </div>
          </div>
          <button
            onClick={() => navigate(`/trips/${tripId}`)}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-white border border-amber-200 text-amber-700 hover:bg-amber-100 text-sm font-bold rounded-lg transition-colors"
          >
            <X size={16} />
            Huỷ bỏ
          </button>
        </div>
      )}

      <div className="flex flex-1 overflow-hidden relative">
        {/* Full Screen Map Background */}
        <div
          className={`absolute inset-0 z-0 bg-slate-200 transition-transform duration-500 ${!showMapOnMobile ? "translate-y-full lg:translate-y-0" : ""}`}
        >
          <Suspense
            fallback={
              <div className="bg-slate-200 animate-pulse w-full h-full" />
            }
          >
            <MapContainer
              ref={mapRef}
              locations={paginatedLocations}
              flyToLocation={flyToLocation}
              userPosition={isNearbyMode ? position : null}
              onMarkerClick={handleMapMarkerClick}
              referenceLocation={
                referencePosition
                  ? { ...referencePosition, title: refLoc || "Địa điểm chọn" }
                  : null
              }
            />
          </Suspense>
        </div>

        {/* Unified Glass Panel (Left) */}
        <div
          className={`absolute top-0 bottom-0 left-0 lg:left-4 z-20 w-full lg:w-[480px] xl:w-[500px] lg:my-6 bg-white/95 backdrop-blur-2xl ring-1 ring-white/60 shadow-[0_20px_40px_rgba(0,0,0,0.08)] lg:rounded-3xl flex flex-col overflow-hidden pointer-events-auto transition-transform duration-500 ${showMapOnMobile ? "-translate-y-[150%] lg:translate-y-0" : "translate-y-0"}`}
        >
          {/* Module 1: Unified Command Center Header */}
          <div className="flex flex-col gap-3 p-4 sm:p-5 border-b border-slate-100/80 bg-white/50 z-10 shrink-0">
            {/* Row 1: City Selector & Add Location Ghost Button */}
            <div className="flex justify-between items-center z-40 relative">
              <div className="relative">
                <button
                  onClick={() => setIsCitySelectorOpen(!isCitySelectorOpen)}
                  className="flex items-center gap-2 pr-3 py-2 rounded-xl text-sm font-bold bg-transparent text-slate-700 hover:text-blue-600 transition-all"
                >
                  <MapPin
                    size={18}
                    className={
                      isCitySelectorOpen ? "text-primary" : "text-slate-400"
                    }
                  />
                  {cityName ? cityName : (!isInitCityDone ? "Đang tìm vị trí..." : "Tất cả địa điểm")}
                  <ChevronDown
                    size={14}
                    className={`text-slate-400 transition-transform ${isCitySelectorOpen ? "rotate-180" : ""}`}
                  />
                </button>

                {/* City Popover */}
                <AnimatePresence>
                  {isCitySelectorOpen && (
                    <>
                      <div
                        className="fixed inset-0 z-30 sm:hidden"
                        onClick={() => setIsCitySelectorOpen(false)}
                      />
                      <motion.div
                        initial={{ opacity: 0, scale: 0.95, y: 10 }}
                        animate={{ opacity: 1, scale: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.95, y: 10 }}
                        className="absolute top-full mt-2 left-0 w-[280px] sm:w-[320px] bg-white rounded-2xl shadow-2xl shadow-blue-900/10 ring-1 ring-slate-100 z-50 overflow-hidden"
                      >
                        <div className="p-3 space-y-3">
                          <input
                            type="text"
                            value={citySearch}
                            onChange={(e) => {
                              const val = e.target.value;
                              setCitySearch(val);
                              if (val.length >= 2)
                                void searchCities({
                                  searchText: val,
                                  pageSize: 10,
                                });
                            }}
                            className="w-full px-3 py-2.5 rounded-xl bg-slate-50 ring-1 ring-slate-200 focus:bg-white focus:ring-2 focus:ring-primary/40 focus:outline-none transition-all font-medium text-sm"
                            placeholder="Tìm thành phố..."
                            autoFocus
                          />
                          <button
                            onClick={handleManualLocationClick}
                            className="flex items-center gap-3 w-full p-2.5 rounded-xl hover:bg-blue-50 transition-colors group text-left"
                          >
                            <div className="w-8 h-8 rounded-lg bg-blue-100/50 flex items-center justify-center text-blue-600 group-hover:bg-gradient-primary group-hover:text-white transition-all">
                              <Navigation size={16} className="fill-current" />
                            </div>
                            <div>
                              <p className="font-semibold text-slate-800 text-sm">
                                Vị trí hiện tại
                              </p>
                            </div>
                          </button>
                          {citySearch.length >= 1 && (
                            <div className="max-h-48 overflow-y-auto space-y-1 pr-1">
                              {searchResults?.items?.map((city) => (
                                <button
                                  key={city.id}
                                  onClick={() => {
                                    setCity(city.id, city.name);
                                    setCitySearch(city.name);
                                    setIsCitySelectorOpen(false);
                                    if (city.latitude && city.longitude) {
                                      setFlyToLocation({
                                        latitude: city.latitude,
                                        longitude: city.longitude,
                                        zoom: 12,
                                      });
                                    }
                                  }}
                                  className={`w-full p-2.5 rounded-xl hover:bg-slate-50 transition-all text-left text-sm ${filters.cityId === city.id ? "bg-blue-50 text-blue-600 font-bold" : "text-slate-700"}`}
                                >
                                  {city.name}
                                </button>
                              ))}
                            </div>
                          )}
                        </div>
                      </motion.div>
                    </>
                  )}
                </AnimatePresence>
              </div>

              {/* Ghost Button: Secondary action */}
              <button
                onClick={() => setIsSubmitOpen(true)}
                className="flex items-center gap-1.5 px-3 py-2 bg-transparent hover:bg-slate-100/80 ring-1 ring-slate-200 text-slate-600 hover:text-slate-900 font-semibold text-xs rounded-xl transition-all active:scale-95"
              >
                <Plus size={14} className="stroke-[2.5]" /> Tạo mới
              </button>
            </div>

            {/* Row 2: Search Input & Sort & Filter Buttons */}
            <div className="flex gap-2 relative z-30">
              <div className="relative flex-1">
                <input
                  type="text"
                  placeholder="Nhập tên địa điểm..."
                  className="w-full pl-10 pr-4 py-3 rounded-2xl bg-white ring-1 ring-slate-200 focus:ring-2 focus:ring-primary/40 focus:outline-none transition-all text-sm font-medium placeholder:text-slate-400 shadow-sm"
                  value={searchInput}
                  onChange={handleSearchInputChange}
                />
                <Search
                  className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400"
                  size={16}
                />
              </div>

              <div className="relative shrink-0 flex gap-2">
                {/* Filter Button */}
                <div className="relative">
                  <button
                    onClick={() => {
                      setIsFilterOpen(!isFilterOpen);
                      setIsSortOpen(false);
                    }}
                    className={`h-full flex items-center justify-center aspect-square px-3 rounded-2xl transition-all shadow-sm ring-1 ring-slate-200 ${isFilterOpen ? "bg-blue-50 ring-blue-300 text-blue-600" : "bg-white hover:bg-slate-50 text-slate-600"}`}
                  >
                    <Filter size={18} />
                  </button>
                  <AnimatePresence>
                    {isFilterOpen && (
                      <motion.div
                        initial={{ opacity: 0, scale: 0.95, y: 10 }}
                        animate={{ opacity: 1, scale: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.95, y: 10 }}
                        className="absolute right-0 top-full mt-2 w-64 bg-white rounded-2xl shadow-xl shadow-blue-900/10 ring-1 ring-slate-100 p-2 z-50 flex flex-col gap-1 max-h-80 overflow-y-auto hide-scrollbar"
                      >
                        {categoriesList.map((cat) => (
                          <button
                            key={cat.id || "all"}
                            onClick={() => {
                              handleCategorySelect(cat.id);
                              setIsFilterOpen(false);
                            }}
                            className={`flex items-center gap-2 px-3 py-2 text-sm rounded-xl transition-colors text-left ${
                              filters.categoryId === cat.id
                                ? "bg-blue-50 text-blue-600 font-bold"
                                : "text-slate-600 hover:bg-slate-50"
                            }`}
                          >
                            <span className="shrink-0">
                              <CategoryIcon
                                name={cat.name}
                                iconUrl={cat.iconUrl}
                                size={16}
                                className={
                                  filters.categoryId === cat.id
                                    ? "text-blue-600"
                                    : "text-slate-400"
                                }
                              />
                            </span>
                            <span className="flex-1 truncate">{cat.name}</span>
                          </button>
                        ))}
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>

                {/* Sort Button */}
                <div className="relative">
                  <button
                    onClick={() => {
                      setIsSortOpen(!isSortOpen);
                      setIsFilterOpen(false);
                    }}
                    className={`h-full flex items-center justify-center aspect-square px-3 rounded-2xl transition-all shadow-sm ring-1 ring-slate-200 ${isSortOpen ? "bg-blue-50 ring-blue-300 text-blue-600" : "bg-white hover:bg-slate-50 text-slate-600"}`}
                  >
                    <SlidersHorizontal size={18} />
                  </button>
                  <AnimatePresence>
                    {isSortOpen && (
                      <motion.div
                        initial={{ opacity: 0, scale: 0.95, y: 10 }}
                        animate={{ opacity: 1, scale: 1, y: 0 }}
                        exit={{ opacity: 0, scale: 0.95, y: 10 }}
                        className="absolute right-0 top-full mt-2 w-48 bg-white rounded-2xl shadow-xl shadow-blue-900/10 ring-1 ring-slate-100 p-2 z-50 flex flex-col gap-1"
                      >
                        {[
                          { value: "name", label: "Từ A-Z" },
                          { value: "rating", label: "Đánh giá cao" },
                          { value: "popular", label: "Phổ biến nhất" },
                          { value: "recent", label: "Mới nhất" },
                        ].map((option) => (
                          <button
                            key={option.value}
                            onClick={() => {
                              setSort(option.value, option.value !== "name");
                              setIsSortOpen(false);
                            }}
                            className={`text-left px-3 py-2 text-sm rounded-xl transition-colors ${activeSortOption === option.value ? "bg-blue-50 text-blue-600 font-bold" : "text-slate-600 hover:bg-slate-50"}`}
                          >
                            {option.label}
                          </button>
                        ))}
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>
              </div>
            </div>
          </div>

          {/* Module 2: Results List (1 Column with Horizontal Cards) */}
          <div
            ref={scrollContainerRef}
            className="flex-1 overflow-y-auto p-4 sm:p-5 scroll-smooth hide-scrollbar relative z-0"
          >
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-4 px-1">
              {totalCount} Địa điểm
            </h3>

            {isLocationsLoading ? (
              <div className="flex flex-col gap-4">
                {[...Array(6)].map((_, i) => (
                  <LocationSkeleton key={i} />
                ))}
              </div>
            ) : (
              <div className="space-y-4">
                <div className="flex flex-col gap-4">
                  {paginatedLocations.map((loc, i) => (
                    <motion.div
                      key={loc.id}
                      initial={CARD_INITIAL}
                      animate={CARD_ANIMATE}
                      transition={getCardTransition(i)}
                    >
                      <HorizontalLocationCard
                        {...loc}
                        onMouseEnter={() => handleCardMouseEnter(loc.id)}
                        onMouseLeave={handleCardMouseLeave}
                        onClick={() => handleCardClick(loc)}
                        onAddToCollection={() => handleAddToCollection(loc.id)}
                      />
                    </motion.div>
                  ))}

                  {finalLocations.length === 0 && (
                    <div className="py-16 text-center">
                      <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-4 border border-slate-200">
                        <Search size={24} className="text-slate-400" />
                      </div>
                      <p className="text-slate-700 font-bold">
                        Không tìm thấy địa điểm
                      </p>
                      <p className="text-slate-500 text-sm mt-1">
                        Hãy thử đổi danh mục hoặc từ khóa
                      </p>
                    </div>
                  )}
                </div>

                {totalPages > 1 && (
                  <div className="mt-8 pt-6 border-t border-slate-200/50 pb-44 lg:pb-12 flex flex-col items-center gap-6">
                    {/* Balanced Pagination Row */}
                    <div className="flex items-center justify-center gap-1 sm:gap-2 w-full px-2">
                      {/* First Page Button */}
                      <button
                        onClick={() => setPage(1)}
                        disabled={currentPage === 1}
                        className="w-9 h-9 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-30 transition-all shadow-sm active:scale-95 shrink-0"
                        title="Trang đầu"
                      >
                        <ChevronsLeft size={18} />
                      </button>

                      {/* Prev Button */}
                      <button
                        onClick={() => {
                          if (currentPage > 1) setPage(currentPage - 1);
                        }}
                        disabled={currentPage === 1}
                        className="w-9 h-9 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 disabled:opacity-30 transition-all shadow-sm active:scale-95 shrink-0"
                        aria-label="Trang trước"
                      >
                        <ChevronDown size={18} className="rotate-90" />
                      </button>

                      {/* Numbers */}
                      <div className="flex items-center justify-center gap-1.5 overflow-x-auto no-scrollbar py-2">
                        {visiblePages.map((num, idx) =>
                          num === "..." ? (
                            <span
                              key={`ellipsis-${idx}`}
                              className="w-9 h-9 flex items-center justify-center text-slate-400 font-bold"
                            >
                              ...
                            </span>
                          ) : (
                            <button
                              key={num}
                              onClick={() => setPage(num)}
                              className={`w-9 h-9 flex items-center justify-center rounded-lg text-xs font-bold transition-all shrink-0 ${
                                currentPage === num
                                  ? "bg-gradient-primary text-white active:scale-95 shadow-sm"
                                  : "bg-white border border-slate-200 text-slate-500 hover:bg-slate-50 hover:border-slate-300 shadow-xs"
                              }`}
                            >
                              {num}
                            </button>
                          ),
                        )}
                      </div>

                      {/* Next Button */}
                      <button
                        onClick={() => {
                          if (currentPage < totalPages)
                            setPage(currentPage + 1);
                        }}
                        disabled={currentPage === totalPages}
                        className="w-10 h-10 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-700 hover:bg-slate-50 disabled:opacity-30 transition-all shadow-sm active:scale-95 shrink-0"
                        aria-label="Trang tiếp"
                      >
                        <ChevronDown size={18} className="-rotate-90" />
                      </button>

                      {/* Last Page Button */}
                      <button
                        onClick={() => setPage(totalPages)}
                        disabled={currentPage === totalPages}
                        className="w-9 h-9 flex items-center justify-center rounded-xl bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-30 transition-all shadow-sm active:scale-95 shrink-0"
                        title="Trang cuối"
                      >
                        <ChevronsRight size={18} />
                      </button>
                    </div>

                    {/* Simple Centered Indicator */}
                    <div className="flex items-center gap-2 opacity-50">
                      <span className="text-[10px] font-bold text-slate-500 uppercase tracking-widest">
                        Trang {currentPage} / {totalPages}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Module 3: Mobile View Toggle Button (Floating Bottom Center) */}
        <div className="absolute bottom-6 lg:hidden left-1/2 -translate-x-1/2 z-30 pointer-events-auto">
          <button
            onClick={() => setShowMapOnMobile(!showMapOnMobile)}
            className="flex items-center gap-2 bg-gradient-primary text-white px-5 py-3 rounded-full font-bold text-sm shadow-[0_8px_20px_rgba(0,0,0,0.4)] ring-1 ring-white/20 border-none hover:scale-105 active:scale-95 transition-all"
          >
            {showMapOnMobile ? (
              <>
                <List size={16} /> Danh sách
              </>
            ) : (
              <>
                <MapIcon size={16} /> Bản đồ
              </>
            )}
          </button>
        </div>
      </div>

      {/* Confirm Alternative Modal */}
      <AnimatePresence>
        {confirmAltLocation && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6">
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
              onClick={() => !isSubmitAltLoading && setConfirmAltLocation(null)}
            />
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 10 }}
              transition={{ type: "spring", stiffness: 300, damping: 25 }}
              className="relative w-full max-w-sm bg-white rounded-[2rem] shadow-[0_20px_40px_-15px_rgba(0,0,0,0.1)] p-8 overflow-hidden"
            >
              <div className="flex justify-between items-start mb-6">
                <div className="w-12 h-12 rounded-2xl bg-amber-100 flex items-center justify-center text-amber-600 mb-4 shrink-0">
                  <MapPin size={24} weight="fill" />
                </div>
                <button
                  onClick={() => setConfirmAltLocation(null)}
                  disabled={isSubmitAltLoading}
                  className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-full transition-colors disabled:opacity-50"
                  aria-label="Đóng"
                >
                  <X size={20} />
                </button>
              </div>

              <h3 className="text-xl font-bold text-slate-900 tracking-tight mb-2">
                Thêm điểm dự phòng
              </h3>
              <p className="text-slate-500 text-sm leading-relaxed mb-8">
                Bạn có chắc chắn muốn chọn{" "}
                <span className="font-semibold text-slate-800">
                  {confirmAltLocation.title}
                </span>{" "}
                làm địa điểm dự phòng cho{" "}
                <span className="font-semibold text-slate-800">
                  {refLoc || "địa điểm này"}
                </span>
                ?
              </p>

              <div className="flex gap-3">
                <button
                  onClick={() => setConfirmAltLocation(null)}
                  disabled={isSubmitAltLoading}
                  className="flex-1 px-4 py-3 rounded-xl font-semibold text-slate-600 bg-slate-50 hover:bg-slate-100 active:scale-[0.98] transition-all disabled:opacity-50"
                >
                  Hủy
                </button>
                <button
                  onClick={() => handlePickAlternative(confirmAltLocation)}
                  disabled={isSubmitAltLoading}
                  className="flex-1 relative px-4 py-3 rounded-xl font-semibold text-white bg-amber-500 hover:bg-amber-600 active:scale-[0.98] transition-all disabled:opacity-50 overflow-hidden flex items-center justify-center"
                >
                  <AnimatePresence mode="popLayout" initial={false}>
                    {isSubmitAltLoading ? (
                      <motion.div
                        key="loading"
                        initial={{ opacity: 0, y: -20 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: 20 }}
                        className="flex items-center gap-2"
                      >
                        <svg
                          className="animate-spin h-5 w-5 text-white"
                          viewBox="0 0 24 24"
                          fill="none"
                        >
                          <circle
                            className="opacity-25"
                            cx="12"
                            cy="12"
                            r="10"
                            stroke="currentColor"
                            strokeWidth="4"
                          ></circle>
                          <path
                            className="opacity-75"
                            fill="currentColor"
                            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                          ></path>
                        </svg>
                        <span>Đang xử lý</span>
                      </motion.div>
                    ) : (
                      <motion.span
                        key="default"
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={{ opacity: 0, y: -20 }}
                      >
                        Đồng ý
                      </motion.span>
                    )}
                  </AnimatePresence>
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>

      <DestinationDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        data={selectedDestination}
        onAction={
          pickAlternativeMode
            ? () => handlePickAlternative(selectedDestination)
            : null
        }
        actionLabel="Chọn làm địa điểm dự phòng"
      />

      {isSubmitOpen && (
        <Suspense fallback={null}>
          <SubmitLocationModal
            isOpen={isSubmitOpen}
            onClose={() => setIsSubmitOpen(false)}
            onSuccess={() => {}}
          />
        </Suspense>
      )}

      {collectionModalState.isOpen && (
        <Suspense fallback={null}>
          <AddToCollectionModal
            isOpen={collectionModalState.isOpen}
            onClose={() =>
              setCollectionModalState({ isOpen: false, locationId: null })
            }
            locationId={collectionModalState.locationId}
          />
        </Suspense>
      )}
    </div>
  );
};

export default ExplorePage;
