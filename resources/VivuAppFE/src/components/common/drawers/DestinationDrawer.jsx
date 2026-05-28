import React, { useState, useEffect, Suspense, useMemo, useCallback, memo } from "react";
import {
  X,
  MapPin,
  Star,
  Clock,
  Globe,
  Phone,
  Flag,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
import PropTypes from "prop-types";
import { useLocationById } from "../../../hooks/locations/useLocations";

const TABS = [
  { id: "overview", label: "Tổng quan" },
  { id: "activities", label: "Hoạt động" },
  { id: "blogs", label: "Blog liên quan" },
  { id: "reviews", label: "Đánh giá" },
  { id: "location", label: "Vị trí" },
];

const ReportLocationModal = React.lazy(
  () => import("../../user/locations/ReportLocationModal"),
);

const DestinationSkeleton = memo(() => (
  <div className="animate-pulse space-y-8">
    {/* Image Gallery Skeleton */}
    <div className="p-6 pb-0">
      <div className="grid grid-cols-4 grid-rows-2 gap-2 rounded-2xl overflow-hidden h-[300px]">
        <div className="col-span-2 row-span-2 bg-gray-200 dark:bg-gray-800" />
        <div className="bg-gray-200 dark:bg-gray-800" />
        <div className="bg-gray-200 dark:bg-gray-800" />
        <div className="bg-gray-200 dark:bg-gray-800" />
        <div className="bg-gray-200 dark:bg-gray-800" />
      </div>
    </div>

    {/* Content Skeleton */}
    <div className="p-6 space-y-12">
      <div className="space-y-6">
        {/* Intro Section */}
        <section className="space-y-3">
          <div className="h-6 bg-gray-200 dark:bg-gray-800 rounded-full w-32 mb-4" />
          <div className="space-y-2">
            <div className="h-4 bg-gray-100 dark:bg-gray-800/50 rounded w-full" />
            <div className="h-4 bg-gray-100 dark:bg-gray-800/50 rounded w-full" />
            <div className="h-4 bg-gray-100 dark:bg-gray-800/50 rounded w-3/4" />
          </div>
        </section>

        {/* Contact Info Skeleton */}
        <section className="space-y-6">
          <div className="flex items-center gap-2">
            <div className="w-1 h-6 bg-gray-200 dark:bg-gray-800 rounded-full" />
            <div className="h-6 bg-gray-200 dark:bg-gray-800 rounded-full w-48" />
          </div>
          <div className="bg-gray-50 dark:bg-white/5 rounded-2xl border border-gray-100 dark:border-gray-800 p-6 h-32 flex items-center justify-around">
            <div className="space-y-2 flex flex-col items-center">
              <div className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-800" />
              <div className="h-3 bg-gray-100 dark:bg-gray-800/50 rounded w-16" />
            </div>
            <div className="space-y-2 flex flex-col items-center">
              <div className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-800" />
              <div className="h-3 bg-gray-100 dark:bg-gray-800/50 rounded w-16" />
            </div>
            <div className="space-y-2 flex flex-col items-center">
              <div className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-800" />
              <div className="h-3 bg-gray-100 dark:bg-gray-800/50 rounded w-16" />
            </div>
          </div>
        </section>

        {/* Tags Skeleton */}
        <div className="flex flex-wrap gap-2">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              className="h-7 w-20 bg-gray-200 dark:bg-gray-800 rounded-full"
            />
          ))}
        </div>
      </div>
    </div>
  </div>
));
DestinationSkeleton.displayName = "DestinationSkeleton";

/**
 * CSS-only styles for GPU-accelerated drawer animation.
 * - `will-change: transform` promotes to compositor layer
 * - `transition` on transform uses GPU, not main thread
 * - No framer-motion = no JS animation overhead
 */
const drawerStyles = {
  backdrop: {
    position: "fixed",
    inset: "0",
    top: "3.5rem", // top-14
    backgroundColor: "rgba(0,0,0,0)",
    zIndex: 60,
    cursor: "pointer",
    transition: "background-color 200ms ease-out",
    pointerEvents: "none",
  },
  backdropOpen: {
    backgroundColor: "rgba(0,0,0,0.4)",
    pointerEvents: "auto",
  },
  panel: {
    position: "fixed",
    top: "3.5rem", // top-14
    right: 0,
    bottom: 0,
    backgroundColor: "white",
    boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.25)",
    zIndex: 70,
    overflow: "hidden",
    display: "flex",
    flexDirection: "column",
    borderLeft: "1px solid #e5e7eb",
    // GPU-accelerated animation
    transform: "translateX(100%)",
    willChange: "transform",
    transition: "transform 280ms cubic-bezier(0.32, 0.72, 0, 1)",
  },
  panelOpen: {
    transform: "translateX(0)",
  },
};

// Responsive width classes applied via className
const PANEL_WIDTH_CLASS = "w-full md:w-3/4 lg:w-1/2";

const DestinationDrawer = ({
  isOpen,
  onClose,
  data,
  side = "right",
  hasBackdrop = true,
  onAction,
  actionLabel = "Xác nhận",
}) => {
  const [isReportOpen, setIsReportOpen] = useState(false);
  const [showFullHours, setShowFullHours] = useState(false);

  const targetLocationId = data?.locationId || data?.id;

  const { data: detailData, isLoading } = useLocationById(
    isOpen ? targetLocationId : null,
  );
  const fullData = detailData || data;

  const parsedImages = useMemo(() => {
    let imgs = [];
    try {
      const field = fullData?.locationDetail?.images;
      if (typeof field === "string") {
        const parsed = JSON.parse(field);
        imgs = Array.isArray(parsed)
          ? parsed.map((item) => (typeof item === "object" ? item.url : item))
          : [];
      } else if (Array.isArray(field)) {
        imgs = field.map((item) =>
          typeof item === "object" ? item.url : item,
        );
      }
    } catch (err) {
      console.error("Gallery parsing error:", err);
    }

    if (imgs.length > 0) return imgs;
    return [
      data?.img ||
        "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?q=80&w=400&auto=format&fit=crop",
    ];
  }, [fullData?.locationDetail?.images, data?.img]);

  const parsedTags = useMemo(() => {
    try {
      const field = fullData?.locationDetail?.tags;
      if (!field) return [];
      if (typeof field === "string") {
        if (field.startsWith("[")) return JSON.parse(field);
        return field.split(",").map((t) => t.trim());
      }
      return Array.isArray(field) ? field : [];
    } catch {
      return [];
    }
  }, [fullData?.locationDetail?.tags]);

  const openingHours = useMemo(() => {
    try {
      const field = fullData?.locationDetail?.openingHours;
      if (!field) return null;
      if (typeof field === "string" && field.trim().startsWith("{")) {
        return JSON.parse(field);
      }
      return field;
    } catch {
      return null;
    }
  }, [fullData?.locationDetail?.openingHours]);

  // Lock body scroll when drawer is open
  useEffect(() => {
    if (isOpen && hasBackdrop) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "unset";
    }
    return () => {
      document.body.style.overflow = "unset";
    };
  }, [isOpen, hasBackdrop]);

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  const handleToggleHours = useCallback(() => {
    setShowFullHours((prev) => !prev);
  }, []);

  const handleOpenReport = useCallback(() => {
    setIsReportOpen(true);
  }, []);

  const handleCloseReport = useCallback(() => {
    setIsReportOpen(false);
  }, []);

  // Don't render anything if no data at all
  if (!data) return null;

  return (
    <>
      {/* Backdrop — CSS transition only, no JS animation */}
      {hasBackdrop && (
        <div
          style={{
            ...drawerStyles.backdrop,
            ...(isOpen ? drawerStyles.backdropOpen : {}),
            // lg:top-16
            top: window.innerWidth >= 1024 ? "4rem" : "3.5rem",
          }}
          onClick={isOpen ? handleClose : undefined}
          aria-hidden="true"
        />
      )}

      {/* Panel — GPU-accelerated translateX, no framer-motion */}
      <div
        style={{
          ...drawerStyles.panel,
          ...(isOpen ? drawerStyles.panelOpen : {}),
          top: window.innerWidth >= 1024 ? "4rem" : "3.5rem",
        }}
        className={PANEL_WIDTH_CLASS}
        role="dialog"
        aria-modal={isOpen}
        aria-hidden={!isOpen}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-100 dark:border-gray-800 bg-white dark:bg-slate-900 sticky top-0 z-30">
          <div className="flex-1 min-w-0 pr-4">
            <h2 className="text-xl sm:text-2xl font-bold text-gray-900 dark:text-white truncate">
              {fullData?.name || data.name}
            </h2>
            <div className="flex items-center text-sm text-gray-500 mt-1">
              <MapPin className="w-4 h-4 mr-1 text-primary" />
              <span className="truncate">
                {fullData?.city?.name || data.province || "Việt Nam"}
              </span>
            </div>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {onAction && (
              <button
                onClick={() => {
                  onAction?.(fullData);
                  handleClose();
                }}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-primary text-white text-xs font-bold rounded-full shadow-md shadow-blue-500/10 hover:shadow-blue-500/25 hover:scale-105 active:scale-95 transition-all mr-1"
              >
                <Star className="w-3.5 h-3.5 fill-current" />
                <span className="hidden sm:inline">
                  {actionLabel || "Chọn dự phòng"}
                </span>
                <span className="sm:hidden">
                  {actionLabel ? "Chọn" : "Chọn"}
                </span>
              </button>
            )}
            <button
              onClick={handleOpenReport}
              className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-gray-400 hover:text-gray-600"
              title="Báo cáo địa điểm"
            >
              <Flag className="w-4 h-4 sm:w-5 sm:h-5" />
            </button>
            <div className="w-px h-4 bg-gray-200 dark:bg-gray-700 mx-1"></div>
            <button
              onClick={handleClose}
              className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
              aria-label="Close"
            >
              <X className="w-5 h-5 sm:w-6 sm:h-6 text-gray-500" />
            </button>
          </div>
        </div>

        {/* Scrollable Content — content-visibility: auto for off-screen optimization */}
        <div
          className="flex-1 overflow-y-auto scroll-smooth relative"
          id="drawer-scroll-container"
          style={{ contentVisibility: "auto" }}
        >
          {isLoading ? (
            <DestinationSkeleton />
          ) : (
            <>
              {/* Image Gallery */}
              <div className="p-6 pb-0">
                <div
                  className={`grid gap-2 rounded-2xl overflow-hidden h-[300px] 
                    ${
                      parsedImages.length === 1
                        ? "grid-cols-1"
                        : parsedImages.length === 2
                          ? "grid-cols-2"
                          : parsedImages.length === 3
                            ? "grid-cols-3"
                            : "grid-cols-4 grid-rows-2"
                    }`}
                >
                  {/* Primary Image */}
                  <div
                    className={`relative overflow-hidden 
                      ${
                        parsedImages.length === 1
                          ? ""
                          : parsedImages.length === 2
                            ? ""
                            : parsedImages.length === 3
                              ? "col-span-2 row-span-2"
                              : "col-span-2 row-span-2"
                      }`}
                  >
                    <img
                      src={parsedImages[0]}
                      alt="Main"
                      className="w-full h-full object-cover hover:scale-105 transition-transform duration-500 cursor-pointer"
                      loading="lazy"
                    />
                  </div>

                  {/* Secondary Images */}
                  {parsedImages.length === 2 && (
                    <div className="relative overflow-hidden">
                      <img
                        src={parsedImages[1]}
                        alt="Side"
                        className="w-full h-full object-cover"
                        loading="lazy"
                      />
                    </div>
                  )}

                  {parsedImages.length === 3 && (
                    <div className="grid grid-rows-2 gap-2">
                      <img
                        src={parsedImages[1]}
                        className="w-full h-full object-cover"
                        loading="lazy"
                      />
                      <img
                        src={parsedImages[2]}
                        className="w-full h-full object-cover"
                        loading="lazy"
                      />
                    </div>
                  )}

                  {parsedImages.length >= 4 && (
                    <>
                      {parsedImages
                        .slice(1, parsedImages.length === 4 ? 4 : 5)
                        .map((img, idx) => (
                          <div key={idx} className="relative overflow-hidden">
                            <img
                              src={img}
                              alt={`Gallery ${idx}`}
                              className="w-full h-full object-cover hover:scale-110 transition-transform duration-300 cursor-pointer"
                              loading="lazy"
                            />
                            {idx === 3 && parsedImages.length > 5 && (
                              <div className="absolute inset-0 bg-black/40 flex items-center justify-center text-white font-bold text-lg pointer-events-none">
                                +{parsedImages.length - 5}
                              </div>
                            )}
                          </div>
                        ))}
                    </>
                  )}
                </div>
              </div>

              {/* Stacked Sections */}
              <div className="p-6 space-y-12 pb-20">
                <div id="overview" className="space-y-6 scroll-mt-20">
                  <div className="space-y-6">
                    <section>
                      <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2">
                        Giới thiệu
                      </h3>
                      <p className="text-gray-600 dark:text-gray-300 leading-relaxed text-sm whitespace-pre-line">
                        {fullData?.description ||
                          data.description ||
                          "Đang cập nhật giới thiệu chi tiết cho địa điểm này."}
                      </p>
                    </section>

                    <section>
                      <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
                        <span className="w-1 h-6 bg-gradient-primary rounded-full"></span>
                        Thông tin liên hệ
                      </h3>

                      <div className="bg-gray-50 dark:bg-white/5 rounded-2xl border border-gray-100 dark:border-gray-800 p-4 flex flex-col md:flex-row gap-6 md:gap-0 divide-y md:divide-y-0 md:divide-x divide-gray-200 dark:divide-gray-700">
                        {/* Address */}
                        <div className="flex-1 flex items-center md:pr-6 gap-3">
                          <div className="w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400 shrink-0">
                            <MapPin className="w-5 h-5" />
                          </div>
                          <div>
                            <p className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-0.5">
                              Địa chỉ
                            </p>
                            <p className="text-sm font-semibold text-gray-900 dark:text-white">
                              {fullData?.address ||
                                data.address ||
                                "Đang cập nhật"}
                            </p>
                          </div>
                        </div>

                        {/* Website & Phone */}
                        <div className="md:px-6 flex flex-col justify-center gap-3 md:w-1/3 py-4 md:py-0">
                          <div className="flex items-center gap-3">
                            <Globe className="w-4 h-4 text-cyan-500 shrink-0" />
                            <span className="text-sm font-semibold truncate">
                              {fullData?.locationDetail?.website || "N/A"}
                            </span>
                          </div>
                          <div className="flex items-center gap-3">
                            <Phone className="w-4 h-4 text-green-500 shrink-0" />
                            <span className="text-sm font-semibold">
                              {fullData?.locationDetail?.phone || "N/A"}
                            </span>
                          </div>
                        </div>

                        {/* Opening Hours Info */}
                        <div className="flex-1 flex md:pl-6 gap-3 group relative">
                          <div className="w-10 h-10 rounded-full bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center text-amber-600 dark:text-amber-400 shrink-0 mt-1">
                            <Clock className="w-5 h-5" />
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center justify-between">
                              <p className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-0.5">
                                Giờ mở cửa
                              </p>
                              {openingHours &&
                                typeof openingHours === "object" && (
                                  <button
                                    onClick={handleToggleHours}
                                    className="text-xs font-bold text-primary hover:underline flex items-center gap-0.5"
                                  >
                                    {showFullHours ? "Thu gọn" : "Chi tiết"}
                                    {showFullHours ? (
                                      <ChevronUp size={12} />
                                    ) : (
                                      <ChevronDown size={12} />
                                    )}
                                  </button>
                                )}
                            </div>

                            <div className="space-y-1">
                              {openingHours &&
                              typeof openingHours === "object" ? (
                                <>
                                  {!showFullHours ? (
                                    <p className="text-sm font-semibold text-gray-900 dark:text-white">
                                      {openingHours[
                                        new Date()
                                          .toLocaleDateString("en-US", {
                                            weekday: "long",
                                          })
                                          .toLowerCase()
                                      ] || "Mở cửa cả ngày"}
                                    </p>
                                  ) : (
                                    <div className="mt-2 space-y-1.5 border-t border-gray-100 dark:border-gray-800 pt-2">
                                      {[
                                        "monday",
                                        "tuesday",
                                        "wednesday",
                                        "thursday",
                                        "friday",
                                        "saturday",
                                        "sunday",
                                      ].map((day) => (
                                        <div
                                          key={day}
                                          className="flex justify-between items-center text-sm"
                                        >
                                          <span className="capitalize text-gray-500">
                                            {day.slice(0, 3)}:
                                          </span>
                                          <span
                                            className={`font-medium ${new Date().toLocaleDateString("en-US", { weekday: "long" }).toLowerCase() === day ? "text-primary font-bold" : "text-gray-900 dark:text-gray-200"}`}
                                          >
                                            {openingHours[day]}
                                          </span>
                                        </div>
                                      ))}
                                    </div>
                                  )}
                                </>
                              ) : (
                                <p className="text-sm font-semibold text-gray-900 dark:text-white">
                                  {fullData?.locationDetail?.openingHours ||
                                    "00:00 - 23:59"}
                                </p>
                              )}
                              {!showFullHours && (
                                <p className="text-xs text-gray-500">
                                  Nhấn chi tiết để xem lịch tuần
                                </p>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>
                    </section>

                    {parsedTags.length > 0 && (
                      <div className="flex flex-wrap gap-2">
                        {parsedTags.map((tag, i) => (
                          <span
                            key={i}
                            className="px-3 py-1 bg-primary/10 text-primary rounded-full text-xs font-semibold"
                          >
                            #{tag.trim()}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Vercel Rule: bundle-conditional */}
      {isReportOpen && (
        <Suspense fallback={null}>
          <ReportLocationModal
            isOpen={isReportOpen}
            onClose={handleCloseReport}
            locationId={targetLocationId}
            locationName={fullData?.name || data?.name}
          />
        </Suspense>
      )}
    </>
  );
};

DestinationDrawer.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  data: PropTypes.object,
  side: PropTypes.oneOf(["left", "right"]),
  hasBackdrop: PropTypes.bool,
  onAction: PropTypes.func,
  actionLabel: PropTypes.string,
};

export default memo(DestinationDrawer);
