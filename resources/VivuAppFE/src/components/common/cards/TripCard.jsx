import React, { memo, useState, startTransition } from "react";
import { useNavigate } from "react-router-dom";
// eslint-disable-next-line no-unused-vars
import { motion } from "framer-motion";
import {
  Users,
  Calendar,
  MapPin,
  Heart,
  User,
  Copy,
  Loader2,
} from "lucide-react";
import { useAuth } from "../../../contexts/auth-context";
import tripApi from "../../../api/tripApi";
import useFavoriteTrip from "../../../hooks/trips/useFavoriteTrip";
import toast from "../../../utils/toast";

/**
 * Postcard style for Trips
 * Split layout: landscape image on top, white info panel on bottom.
 * Structured chips. Aspect ratio 16:10 for the image container or card.
 */
const TripCard = memo(
  ({
    trip,
    isFavorited: isFavoritedProp,
    variants,
    className = "",
    large = false,
  }) => {
    const navigate = useNavigate();
    const { userId } = useAuth();
    const [isCopying, setIsCopying] = useState(false);

    // Local state for optimistic UI updates
    const [localFavoritesCount, setLocalFavoritesCount] = useState(
      trip?.favoritesCount || 0,
    );

    const { isFavorited, toggleFavorite, isLoading } = useFavoriteTrip(
      trip?.id,
      isFavoritedProp ?? trip?.isFavorited ?? false,
    );

    const handleMouseEnter = () => {
      import("../../../pages/user/trips/TripDetailPage");
    };

    const handleClick = (e) => {
      if (e.target.closest("button")) return;
      navigate(`/trips/public/${trip.id}`);
    };

    const handleCopyTrip = async (e) => {
      e.stopPropagation();
      if (!userId) {
        toast.error("Vui lòng đăng nhập để sao chép chuyến đi");
        return;
      }
      if (isCopying) return;
      setIsCopying(true);

      try {
        const res = await tripApi.copy(trip.id);
        const newId = res.data?.id || res.data?.Id;
        toast.success("Đã sao chép chuyến đi!");
        if (newId) {
          startTransition(() => {
            navigate(`/trips/${newId}`);
          });
        }
      } catch {
        toast.error("Không thể sao chép. Vui lòng thử lại.");
      } finally {
        setIsCopying(false);
      }
    };

    const handleFavoriteClick = async (e) => {
      e.stopPropagation();
      if (!userId) {
        toast.error("Vui lòng đăng nhập để thao tác.");
        return;
      }

      if (isLoading) return;

      // Optimistically update count
      setLocalFavoritesCount((prev) => (isFavorited ? prev - 1 : prev + 1));

      // Call API via hook
      const success = await toggleFavorite();

      // Revert count if API fails
      if (!success) {
        setLocalFavoritesCount((prev) => (isFavorited ? prev + 1 : prev - 1));
      }
    };

    if (!trip) return null;

    return (
      <motion.div
        variants={variants}
        onClick={handleClick}
        onMouseEnter={handleMouseEnter}
        className={`group cursor-pointer w-full flex flex-col ${className}`}
      >
        {/* 1. Thumbnail Container */}
        <div className={`relative w-full rounded-2xl overflow-hidden shadow-sm transition-all duration-300 group-hover:shadow-md ${
          large ? "aspect-[4/3] sm:aspect-video" : "aspect-[4/5] sm:aspect-square"
        }`}>
          <img
            src={trip.coverUrl}
            alt={trip.title}
            loading="lazy"
            className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
          />
          <div className="absolute inset-0 bg-black/5 group-hover:bg-black/0 transition-colors duration-300" />

          {/* Top Left Badge: Duration */}
          <div className="absolute top-3 left-3 z-10">
            {trip.durationDays && (
              <div className="bg-white/95 backdrop-blur-md px-2.5 py-1 rounded-full text-[11px] font-bold text-gray-900 flex items-center shadow-sm">
                {trip.durationDays} ngày
              </div>
            )}
          </div>

          {/* Action Buttons - Top Right */}
          <div className="absolute top-3 right-3 z-10 flex items-center gap-1.5">
            <button
              className={`p-1.5 rounded-full shadow-sm transition-all active:scale-95 text-white ${
                isCopying ? "bg-black/50" : "bg-black/30 backdrop-blur-md hover:bg-black/50"
              }`}
              onClick={handleCopyTrip}
              disabled={isCopying}
              title="Sao chép"
            >
              {isCopying ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <Copy size={14} />
              )}
            </button>
            <button
              className={`p-1.5 rounded-full shadow-sm transition-all active:scale-95 ${
                isFavorited
                  ? "bg-red-500 text-white"
                  : "bg-black/30 backdrop-blur-md text-white hover:bg-black/50"
              } ${isLoading ? "opacity-50 cursor-not-allowed" : ""}`}
              onClick={handleFavoriteClick}
              disabled={isLoading}
              title={isFavorited ? "Bỏ yêu thích" : "Yêu thích"}
            >
              <Heart size={14} fill={isFavorited ? "currentColor" : "none"} />
            </button>
          </div>
        </div>

        {/* 2. Text Info (Below Thumbnail) */}
        <div className="mt-3 flex flex-col px-1">
          {/* Title */}
          <h3 className={`font-bold text-gray-900 leading-snug line-clamp-2 transition-colors group-hover:text-blue-600 ${
            large ? "text-xl sm:text-2xl" : "text-base"
          }`}>
            {trip.title}
          </h3>

          {/* Location / Meta */}
          <div className="flex items-center gap-1.5 mt-1.5 text-[13px] text-gray-500 font-medium">
            {trip.cityName && (
              <div className="flex items-center gap-1 truncate">
                <MapPin size={13} className="shrink-0" />
                <span className="truncate">{trip.cityName}</span>
              </div>
            )}
            {trip.cityName && trip.memberCount > 0 && <span className="shrink-0">•</span>}
            {trip.memberCount > 0 && (
              <div className="flex items-center gap-1 shrink-0">
                <Users size={13} />
                <span>{trip.memberCount} tv</span>
              </div>
            )}
          </div>

          {/* Author */}
          <div className="flex items-center gap-1.5 mt-2.5">
            <div className="w-5 h-5 rounded-full bg-gray-100 overflow-hidden flex items-center justify-center shrink-0 border border-gray-200">
              {trip.ownerAvatar ? (
                <img
                  src={trip.ownerAvatar}
                  alt={trip.ownerName}
                  className="w-full h-full object-cover"
                />
              ) : (
                <User size={12} className="text-gray-400" />
              )}
            </div>
            <span className="text-[13px] font-medium text-gray-600 truncate hover:text-gray-900 transition-colors">
              {trip.ownerName}
            </span>
            <span className="text-[12px] text-gray-400 ml-auto shrink-0 font-medium">
              {localFavoritesCount > 0 ? `${localFavoritesCount} lượt thích` : ""}
            </span>
          </div>
        </div>
      </motion.div>
    );
  },
);

TripCard.displayName = "TripCard";
export default TripCard;
