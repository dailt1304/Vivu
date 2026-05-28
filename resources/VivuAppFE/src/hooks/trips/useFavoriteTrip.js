import { useState, useCallback, useEffect } from "react";
import tripApi from "../../api/tripApi";
import toast from "../../utils/toast";

/**
 * Hook for managing favorite status of a trip with optimistic updates
 * @param {string} tripId - The ID of the trip
 * @param {boolean} initialIsFavorited - Initial favorite state
 */
const useFavoriteTrip = (tripId, initialIsFavorited = false) => {
  const [isFavorited, setIsFavorited] = useState(initialIsFavorited);
  const [isLoading, setIsLoading] = useState(false);

  // Sync state if initial value changes (e.g., when trip data is loaded)
  useEffect(() => {
    setIsFavorited(initialIsFavorited);
  }, [initialIsFavorited]);

  const toggleFavorite = useCallback(async () => {
    if (!tripId) return false;

    // Optimistic update
    const previousState = isFavorited;
    setIsFavorited(!previousState);
    setIsLoading(true);

    try {
      if (previousState) {
        await tripApi.unfavorite(tripId);
        toast.success("Đã bổ khỏi danh sách yêu thích!");
      } else {
        await tripApi.favorite(tripId);
        toast.success("Đã thêm vào danh sách yêu thích!");
      }
      return true;
    } catch (error) {
      if (error.response?.status === 409) {
        // Zalo/Discord sync issue or multiple tabs: trip is already favorited on server
        setIsFavorited(true);
        toast.info("Chuyến đi này đã nằm trong danh sách yêu thích.");
        return true;
      } else {
        // Revert on real error
        setIsFavorited(previousState);
        toast.error("Không thể thay đổi trạng thái yêu thích");
        console.error("Lỗi toggle favorite:", error);
        return false;
      }
    } finally {
      setIsLoading(false);
    }
  }, [tripId, isFavorited]);

  return { isFavorited, toggleFavorite, isLoading };
};

export default useFavoriteTrip;
