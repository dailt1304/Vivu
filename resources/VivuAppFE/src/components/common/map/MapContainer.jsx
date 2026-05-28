import React, {
  useEffect,
  useRef,
  useState,
  forwardRef,
  useImperativeHandle,
  useLayoutEffect,
} from "react";
import goongjs from "@goongmaps/goong-js";
import "@goongmaps/goong-js/dist/goong-js.css";
import { MapPin, Flag, Navigation } from "lucide-react";

const GOONG_MAP_TILES_KEY = import.meta.env.VITE_GOONG_MAP_TILES_KEY;

const ROUTE_CASING_PAINT = {
  "line-color": "#312e81",
  "line-width": 9,
  "line-opacity": 0.4,
};

const ROUTE_PAINT_CONFIG = {
  "line-color": "#3b82f6",
  "line-width": 4,
  "line-opacity": 1,
};

const ROUTE_GLOW_PAINT = {
  "line-color": "#3b82f6",
  "line-width": 10,
  "line-opacity": 0.25,
};

const ARROW_SIZE = 16;

// Category-based marker configuration
const CATEGORY_CONFIG = {
  "Nature & Park": { color: "emerald", icon: "🌿", hex: "#10b981" },
  Beach: { color: "cyan", icon: "🏖️", hex: "#06b6d4" },
  Hotel: { color: "indigo", icon: "🏨", hex: "#6366f1" },
  Restaurant: { color: "orange", icon: "🍜", hex: "#f97316" },
  "Coffee Shop": { color: "amber", icon: "☕", hex: "#f59e0b" },
  Entertainment: { color: "purple", icon: "🎡", hex: "#a855f7" },
  "Historical Site": { color: "rose", icon: "🏛️", hex: "#f43f5e" },
  "Shopping Mall": { color: "pink", icon: "🛍️", hex: "#ec4899" },
  Default: { color: "blue", icon: "📍", hex: "#3b82f6" },
};

const getCategoryConfig = (categoryName) => {
  return CATEGORY_CONFIG[categoryName] || CATEGORY_CONFIG["Default"];
};

const renderMarkerIconHtml = (loc, isScheduled = false) => {
  const { categoryIcon, category } = loc;
  const sizeClass = isScheduled ? "text-base" : "text-sm";

  // URL icon (SVG/PNG)
  if (
    categoryIcon &&
    (categoryIcon.startsWith("http") || categoryIcon.startsWith("/"))
  ) {
    return `<img src="${categoryIcon}" class="w-full h-full object-contain p-1" alt="${category || ""}" />`;
  }

  // Emoji icon (Check if it's a non-empty string and not a long URL)
  if (categoryIcon && categoryIcon.length > 0 && categoryIcon.length <= 4) {
    return `<span class="${sizeClass} leading-none flex items-center justify-center w-full h-full pb-0.5">${categoryIcon}</span>`;
  }

  // 3. Fallback to existing config
  const config = getCategoryConfig(category);
  return `<span class="${sizeClass} leading-none bg-${config.color}-50 w-full h-full rounded-full flex items-center justify-center">${config.icon}</span>`;
};

// Create arrow image only once per map instance
const ensureArrowImage = (mapInstance) => {
  if (mapInstance.hasImage("route-arrow")) return;

  const canvas = document.createElement("canvas");
  canvas.width = ARROW_SIZE;
  canvas.height = ARROW_SIZE;
  const ctx = canvas.getContext("2d");

  // Draw solid triangle/arrow head
  ctx.fillStyle = "#ffffff";
  ctx.beginPath();
  ctx.moveTo(2, 2);
  ctx.lineTo(14, 8);
  ctx.lineTo(2, 14);
  ctx.fill();

  mapInstance.addImage("route-arrow", {
    width: ARROW_SIZE,
    height: ARROW_SIZE,
    data: ctx.getImageData(0, 0, ARROW_SIZE, ARROW_SIZE).data,
  });
};

// Polyline decoder algorithm for Google/Goong directions
const decodePolyline = (encoded) => {
  let points = [];
  let index = 0,
    len = encoded.length;
  let lat = 0,
    lng = 0;

  while (index < len) {
    let b,
      shift = 0,
      result = 0;
    do {
      b = encoded.charAt(index++).charCodeAt(0) - 63;
      result |= (b & 0x1f) << shift;
      shift += 5;
    } while (b >= 0x20);
    let dlat = result & 1 ? ~(result >> 1) : result >> 1;
    lat += dlat;

    shift = 0;
    result = 0;
    do {
      b = encoded.charAt(index++).charCodeAt(0) - 63;
      result |= (b & 0x1f) << shift;
      shift += 5;
    } while (b >= 0x20);
    let dlng = result & 1 ? ~(result >> 1) : result >> 1;
    lng += dlng;

    // Output is [longitude, latitude] for GeoJSON
    points.push([lng * 1e-5, lat * 1e-5]);
  }
  return points;
};

const MapContainer = forwardRef(
  (
    {
      locations = [],
      onMarkerClick,
      flyToLocation,
      scheduledItemIds,
      routeData,
      userPosition,
      referenceLocation = null,
      hideMarkerNumbers = false,
    },
    ref,
  ) => {
    const mapContainerRef = useRef(null);
    const [map, setMap] = useState(null);
    const markersRef = useRef([]);

    const onMarkerClickRef = useRef(onMarkerClick);
    useLayoutEffect(() => {
      onMarkerClickRef.current = onMarkerClick;
    });

    useEffect(() => {
      if (!GOONG_MAP_TILES_KEY) return;

      goongjs.accessToken = GOONG_MAP_TILES_KEY;

      const mapInstance = new goongjs.Map({
        container: mapContainerRef.current,
        style: "https://tiles.goong.io/assets/goong_map_web.json",
        center: [108.20623, 16.047079], // Da Nang
        zoom: 5,
      });

      mapInstance.addControl(new goongjs.NavigationControl(), "top-right");
      mapInstance.addControl(new goongjs.FullscreenControl(), "top-left");

      mapInstance.on("load", () => {
        setMap(mapInstance);
        mapInstance.resize(); // Initial resize
      });

      const resizeObserver = new ResizeObserver(() => {
        mapInstance.resize();
      });

      if (mapContainerRef.current) {
        resizeObserver.observe(mapContainerRef.current);
      }

      return () => {
        mapInstance.remove();
        resizeObserver.disconnect();
      };
    }, []);

    // Update markers when map is loaded or locations change
    useEffect(() => {
      if (!map || !locations) return;

      // Clear old markers
      markersRef.current.forEach((marker) => marker.remove());
      markersRef.current = [];

      // Tracks counts of markers at each coordinate to handle overlaps
      const coordinateGroups = new Map();

      // Group locations by coordinate to handle overlaps
      locations.forEach((loc) => {
        if (!loc.latitude || !loc.longitude) return;
        const key = `${loc.latitude.toFixed(6)},${loc.longitude.toFixed(6)}`;
        if (!coordinateGroups.has(key)) coordinateGroups.set(key, []);
        coordinateGroups.get(key).push(loc);
      });

      // Determine rendering loop data
      const renderData = Array.from(coordinateGroups.values()).map((group) => ({
        isGroup: group.length > 1,
        items: group,
        locIds: group.map(i => i.id),
        ...group[0], // Use representative properties from the first item
      }));

      renderData.forEach((data) => {
        if (!data.latitude || !data.longitude) return;

        const lng = data.longitude;
        const lat = data.latitude;

        const isScheduled = data.items.some((i) => scheduledItemIds?.has(i.id));

        const badgeText = data.items.map((i) => i.orderIndex || 1).join(", ");

        // Create a DOM element for the marker
        const el = document.createElement("div");
        el.className = "custom-marker";
        if (!data.isGroup) el.dataset.id = data.id;
        el.style.cursor = "pointer";
        el.style.zIndex = isScheduled ? "100" : "auto";

        // Immediate DOM hover effects
        el.addEventListener("mouseenter", () => {
          el.style.zIndex = "9999";
        });
        el.addEventListener("mouseleave", () => {
          const currentlyHovered = el.dataset.isHovered === "true";
          el.style.zIndex = currentlyHovered
            ? "9999"
            : isScheduled
              ? "100"
              : "auto";
        });

        const config = getCategoryConfig(data.category);
        // Ensure unique titles to avoid redundancy like "Hotel A • Hotel A"
        const uniqueTitles = [...new Set(data.items.map((i) => i.title))];
        const titleText = uniqueTitles.join(" • ");

        const innerHTML = `
        <div class="marker-pin-container marker-pin-inner flex flex-col items-center group relative cursor-pointer transition-transform duration-300 scale-100 hover:scale-110 hover:z-50">
          
          <!-- Teardrop Pin -->
          <div class="relative flex items-center justify-center ${isScheduled ? "w-9 h-9" : "w-8 h-8"} bg-white rounded-full shadow-md border-2" style="border-color: ${config.hex};">
            ${renderMarkerIconHtml(data, isScheduled)}
            <div class="absolute -bottom-1.5 ${isScheduled ? "w-2.5 h-2.5" : "w-2 h-2"} bg-white border-b-2 border-r-2 transform rotate-45" style="border-color: ${config.hex};"></div>
            
            <!-- Badge - Only show when scheduled (itinerary) -->
            ${
              isScheduled && !hideMarkerNumbers
                ? `
            <div class="absolute -top-2 -right-2 bg-indigo-600 text-white min-w-[20px] h-5 rounded-full flex items-center justify-center text-[9px] font-bold border-2 border-white shadow-sm ring-1 ring-indigo-400 px-1">
              ${badgeText}
            </div>
            `
                : ""
            }
          </div>
          
          <!-- Hover Label -->
          <div class="marker-label opacity-0 transform -translate-y-1 group-hover:opacity-100 group-hover:translate-y-0 absolute top-11 bg-white px-2.5 py-1.5 rounded-lg shadow-xl text-xs whitespace-nowrap z-50 pointer-events-none transition-all duration-200 text-gray-800 font-bold border border-gray-100 flex flex-col gap-0.5 min-w-max">
            <div class="flex items-center gap-1.5">
              <span class="inline-block w-2.5 h-2.5 rounded-full shrink-0" style="background-color: ${config.hex};"></span>
              ${titleText}
            </div>
          </div>
        </div>
      `;

        el.innerHTML = innerHTML;

        el.addEventListener("click", (e) => {
          e.stopPropagation();
          onMarkerClickRef.current?.(data.isGroup ? data.items[0] : data);
        });

        const marker = new goongjs.Marker(el).setLngLat([lng, lat]).addTo(map);

        marker._el = el;
        marker._locIds = data.locIds;
        markersRef.current.push(marker);
      });

      // Render Reference Location Marker (if provided)
      if (referenceLocation && referenceLocation.latitude && referenceLocation.longitude) {
        const el = document.createElement("div");
        el.className = "reference-marker";
        el.style.cursor = "pointer";
        el.style.zIndex = "1000";

        const config = { hex: "#ef4444", icon: "🚩", color: "rose" }; // Red Flag style
        
        el.innerHTML = `
          <div class="marker-pin-container flex flex-col items-center group relative cursor-pointer scale-110 active:scale-95 transition-all">
            <div class="relative flex items-center justify-center w-9 h-9 bg-white rounded-full shadow-lg border-2" style="border-color: ${config.hex};">
              <span class="text-base leading-none flex items-center justify-center w-full h-full pb-0.5">${config.icon}</span>
              <div class="absolute -bottom-1.5 w-2.5 h-2.5 bg-white border-b-2 border-r-2 transform rotate-45" style="border-color: ${config.hex};"></div>
              
              <!-- Special Label for Ref - Now only shows on hover -->
              <div class="absolute -top-7 px-2 py-1 bg-red-600 text-white text-[10px] font-bold rounded-md shadow-md whitespace-nowrap animate-bounce opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none">
                Địa điểm chính
              </div>
            </div>
            
            <!-- Hover Label -->
            <div class="marker-label opacity-0 transform -translate-y-1 group-hover:opacity-100 group-hover:translate-y-0 absolute top-11 bg-white px-2.5 py-1.5 rounded-lg shadow-xl text-xs whitespace-nowrap z-50 pointer-events-none transition-all duration-200 text-gray-800 font-bold border border-gray-100 flex flex-col gap-0.5 min-w-max">
              <div class="flex items-center gap-1.5">
                <span class="inline-block w-2.5 h-2.5 rounded-full shrink-0" style="background-color: ${config.hex};"></span>
                ${referenceLocation.title || "Địa điểm gốc"}
              </div>
            </div>
          </div>
        `;

        el.addEventListener("click", (e) => {
          e.stopPropagation();
          onMarkerClickRef.current?.(referenceLocation);
        });

        const marker = new goongjs.Marker(el)
          .setLngLat([referenceLocation.longitude, referenceLocation.latitude])
          .addTo(map);
        
        marker._el = el;
        markersRef.current.push(marker);
      }
    }, [map, locations, scheduledItemIds, referenceLocation, hideMarkerNumbers]);

    // Update hover state purely through DOM classes without recreating markers via ref
    useImperativeHandle(ref, () => ({
      setHovered: (hoveredId) => {
        if (!map || markersRef.current.length === 0) return;

        markersRef.current.forEach((marker) => {
          const el = marker._el;
          if (!el) return;

          const locIds = marker._locIds || [];
          const isHovered = hoveredId ? locIds.includes(hoveredId) : false;
          const innerContainer = el.querySelector(".marker-pin-inner");
          const label = el.querySelector(".marker-label");

          el.dataset.isHovered = isHovered.toString();

          if (isHovered) {
            el.style.zIndex = "9999";
            if (innerContainer) {
              innerContainer.classList.remove("scale-100");
              innerContainer.classList.add("scale-110", "z-50");
            }
            if (label) {
              label.classList.remove("opacity-0", "-translate-y-1");
              label.classList.add("opacity-100", "translate-y-0");
            }
          } else {
            // Reset zIndex based on scheduled status
            const locIds = marker._locIds || [];
            const isScheduled = locIds.some(id => scheduledItemIds?.has(id));
            el.style.zIndex = isScheduled ? "100" : "auto";

            if (innerContainer) {
              innerContainer.classList.remove("scale-110", "z-50");
              innerContainer.classList.add("scale-100");
            }
            if (label) {
              label.classList.remove("opacity-100", "translate-y-0");
              label.classList.add("opacity-0", "-translate-y-1");
            }
          }
        });
      },
    }));

    // User Position Marker
    const userMarkerRef = useRef(null);
    useEffect(() => {
      if (!map || !userPosition) return;

      // Clear old
      if (userMarkerRef.current) userMarkerRef.current.remove();

      const el = document.createElement("div");
      el.innerHTML = `
      <div class="flex flex-col items-center">
        <div class="w-5 h-5 rounded-full bg-blue-500 border-3 border-white shadow-lg relative">
          <div class="absolute inset-0 rounded-full bg-blue-500 animate-ping opacity-30"></div>
        </div>
        <div class="mt-1 bg-white px-2 py-0.5 rounded-full text-[9px] font-bold text-blue-700 shadow-sm border whitespace-nowrap">
          Vị trí của bạn
        </div>
      </div>
    `;
      el.style.zIndex = "999";

      userMarkerRef.current = new goongjs.Marker(el)
        .setLngLat([userPosition.longitude, userPosition.latitude])
        .addTo(map);

      return () => userMarkerRef.current?.remove();
    }, [map, userPosition]);

    // Helper to fit bounds to all locations
    const fitBoundsToLocations = (mapInstance, locs) => {
      if (!locs || locs.length === 0) return;

      const bounds = new goongjs.LngLatBounds();
      let hasValidLoc = false;

      locs.forEach((loc) => {
        if (loc.longitude && loc.latitude) {
          bounds.extend([loc.longitude, loc.latitude]);
          hasValidLoc = true;
        }
      });

      if (hasValidLoc) {
        mapInstance.fitBounds(bounds, {
          padding: 100,
          maxZoom: 15,
          duration: 1000, // Smooth animation
        });
      }
    };

    // Update bounds when locations change or map loads
    useEffect(() => {
      if (map && locations.length > 0) {
        fitBoundsToLocations(map, locations);
      }
    }, [map, locations]);

    /* Handles Hover FlyTo & Reset - Temporarily disabled due to aggressive zoom
  useEffect(() => {
    if (!map) return;

    if (hoveredItemId) {
      const targetLoc = locations.find((l) => l.id === hoveredItemId);
      if (targetLoc && targetLoc.latitude && targetLoc.longitude) {
        map.flyTo({
          center: [targetLoc.longitude, targetLoc.latitude],
          zoom: 16,
          pitch: 45,
          speed: 1.5,
          curve: 1,
          essential: true,
        });
      }
    } else {
      fitBoundsToLocations(map, locations);
    }
  }, [hoveredItemId, map, locations]);
  */

    // Handle Explicit FlyTo
    useEffect(() => {
      if (!map || !flyToLocation) return;

      const { latitude, longitude, zoom } = flyToLocation;
      if (latitude && longitude) {
        map.flyTo({
          center: [longitude, latitude],
          zoom: zoom || 14,
          essential: true,
        });
      }
    }, [map, flyToLocation]);

    // Handle Route Rendering
    useEffect(() => {
      if (!map) return;

      ensureArrowImage(map);

      const sourceId = "directions-route";
      const casingLayerId = "directions-route-casing";
      const glowLayerId = "directions-route-glow";
      const lineLayerId = "directions-route-line";
      const arrowLayerId = "directions-route-arrows";

      // Cleanup existing routes when unmounting or data clears
      const cleanupRoute = () => {
        if (!map || !map.getStyle || !map.getStyle()) return; // Prevent internal Mapbox style crashes
        if (map.getLayer(arrowLayerId)) map.removeLayer(arrowLayerId);
        if (map.getLayer(lineLayerId)) map.removeLayer(lineLayerId);
        if (map.getLayer(glowLayerId)) map.removeLayer(glowLayerId);
        if (map.getLayer(casingLayerId)) map.removeLayer(casingLayerId);
        if (map.getSource(sourceId)) map.removeSource(sourceId);
      };

      if (!routeData?.polylines?.length) {
        cleanupRoute();
        return;
      }

      // Decode all legs and stitch them into a single LineString
      const allCoordinates = [];
      routeData.polylines.forEach((encodedPoly) => {
        const decoded = decodePolyline(encodedPoly);
        // Avoid duplicate points at segment boundaries
        if (allCoordinates.length > 0 && decoded.length > 0) {
          const lastEl = allCoordinates[allCoordinates.length - 1];
          const firstEl = decoded[0];
          if (lastEl[0] === firstEl[0] && lastEl[1] === firstEl[1]) {
            allCoordinates.push(...decoded.slice(1));
          } else {
            allCoordinates.push(...decoded);
          }
        } else {
          allCoordinates.push(...decoded);
        }
      });

      cleanupRoute();

      map.addSource(sourceId, {
        type: "geojson",
        data: {
          type: "Feature",
          properties: {},
          geometry: {
            type: "LineString",
            coordinates: allCoordinates,
          },
        },
      });

      // Casing Layer (Bottom)
      map.addLayer({
        id: casingLayerId,
        type: "line",
        source: sourceId,
        layout: {
          "line-join": "round",
          "line-cap": "round",
        },
        paint: ROUTE_CASING_PAINT,
      });

      // Glow Layer (Neon effect)
      map.addLayer({
        id: glowLayerId,
        type: "line",
        source: sourceId,
        layout: {
          "line-join": "round",
          "line-cap": "round",
        },
        paint: ROUTE_GLOW_PAINT,
      });

      // Main Line Layer (Middle)
      map.addLayer({
        id: lineLayerId,
        type: "line",
        source: sourceId,
        layout: {
          "line-join": "round",
          "line-cap": "round",
        },
        paint: ROUTE_PAINT_CONFIG,
      });

      // Arrow Symbol Layer (Top)
      map.addLayer({
        id: arrowLayerId,
        type: "symbol",
        source: sourceId,
        layout: {
          "symbol-placement": "line",
          "symbol-spacing": 80, // Space between arrows
          "icon-image": "route-arrow",
          "icon-size": 0.8,
          "icon-allow-overlap": true,
        },
      });

      return cleanupRoute;
    }, [map, routeData?.polylines]);

    if (!GOONG_MAP_TILES_KEY) {
      return (
        <div className="flex items-center justify-center h-full bg-gray-100 text-gray-500 flex-col gap-2 p-8 text-center">
          <span className="font-bold text-red-500">Missing API Key</span>
          <p className="text-sm">
            Please add{" "}
            <code className="bg-gray-200 px-1 rounded">
              VITE_GOONG_MAP_TILES_KEY
            </code>{" "}
            to your .env file.
          </p>
        </div>
      );
    }

    return <div ref={mapContainerRef} className="h-full w-full relative" />;
  },
);

export default React.memo(MapContainer);
