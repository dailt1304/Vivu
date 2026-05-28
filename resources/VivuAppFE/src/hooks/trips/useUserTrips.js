import { useState, useEffect, useCallback, useMemo } from "react";
import { format, parseISO, differenceInDays } from "date-fns";
import tripApi from "../../api/tripApi";
import { useAuth } from "../../contexts/auth-context";

/**
 * Custom hook for fetching and managing user's trips
 *
 * @returns {{
 *   trips: Array,
 *   ownedTrips: Array,
 *   sharedTrips: Array,
 *   loading: boolean,
 *   error: string | null,
 *   refetch: () => Promise<void>,
 *   deleteTrip: (id: string) => Promise<boolean>,
 *   createTrip: (data: object) => Promise<object | null>
 * }}
 */
const useUserTrips = () => {
  const { userId, isAuthenticated } = useAuth();

  const [trips, setTrips] = useState([]);
  const [tripLimitInfo, setTripLimitInfo] = useState({
    numberOfTripCreated: 0,
    tripLimit: 0,
    hasReachedLimit: false,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Transform API trip data to UI format
  const transformTrip = useCallback((trip) => {
    const startDate = trip.startDate ? parseISO(trip.startDate) : new Date();
    const endDate = trip.endDate ? parseISO(trip.endDate) : new Date();
    const days = differenceInDays(endDate, startDate);

    const formattedDates = `${format(startDate, "dd")} Th${format(startDate, "MM")} - ${format(endDate, "dd")} Th${format(endDate, "MM")}, ${format(endDate, "yyyy")}`;

    return {
      id: trip.id,
      title: trip.title || "Chuyến đi không tên",
      location: trip.cityName || trip.destination || "Việt Nam",
      dates: formattedDates,
      image:
        trip.coverUrl ||
        "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
      days: days,
      status: trip.status || "planning",
      isDelete: trip.isDeleted || trip.isDelete || false,
      ownerId: trip.userId || trip.ownerId,
      isOwner: trip.isOwner || trip.role === "owner" || trip.role === "Owner",
      startDate,
      endDate,
      rating: trip.rating,
      review: trip.reviewContent,
      isRated: trip.rating != null && trip.rating > 0,
      isPublic: trip.isPublic || false,
    };
  }, []);

  // Fetch trips from API
  const fetchTrips = useCallback(async () => {
    if (!userId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const response = await tripApi.getAllByUser(userId, { pageSize: 100 });

      if (response.success && response.data?.trips?.items) {
        const transformedTrips = response.data.trips.items
          .map(transformTrip)
          .filter((trip) => !trip.isDelete);

        setTrips(transformedTrips);
        setTripLimitInfo({
          numberOfTripCreated: response.data.numberOfTripCreated || 0,
          tripLimit: response.data.tripLimit || 0,
          hasReachedLimit:
            response.data.tripLimit !== -1 &&
            response.data.numberOfTripCreated >= response.data.tripLimit,
        });
      } else {
        setTrips([]);
      }
    } catch (err) {
      console.error("Failed to fetch trips:", err);
      setError("Không thể tải danh sách chuyến đi");
    } finally {
      setLoading(false);
    }
  }, [userId, transformTrip]);

  // Initial fetch
  useEffect(() => {
    if (isAuthenticated) {
      fetchTrips();
    } else {
      setLoading(false);
    }
  }, [isAuthenticated, fetchTrips]);

  // Delete a trip
  const deleteTrip = useCallback(async (tripId) => {
    try {
      await tripApi.delete(tripId);
      setTrips((prev) => prev.filter((t) => t.id !== tripId));
      setTripLimitInfo((prev) => {
        const newCount = Math.max(0, prev.numberOfTripCreated - 1);
        return {
          ...prev,
          numberOfTripCreated: newCount,
          hasReachedLimit: prev.tripLimit !== -1 && newCount >= prev.tripLimit,
        };
      });
      return true;
    } catch (err) {
      console.error("Failed to delete trip:", err);
      return false;
    }
  }, []);

  // Create a new trip
  const createTrip = useCallback(
    async (data) => {
      try {
        const response = await tripApi.create(data);
        if (response.success && response.data) {
          const newTrip = transformTrip(response.data);
          setTrips((prev) => [newTrip, ...prev]);
          setTripLimitInfo((prev) => {
            const newCount = prev.numberOfTripCreated + 1;
            return {
              ...prev,
              numberOfTripCreated: newCount,
              hasReachedLimit: prev.tripLimit !== -1 && newCount >= prev.tripLimit,
            };
          });
          return response.data;
        }
        return null;
      } catch (err) {
        console.error("Failed to create trip:", err);
        throw err;
      }
    },
    [transformTrip],
  );

  // Derived: owned vs shared trips
  const ownedTrips = useMemo(
    () => trips.filter((t) => t.ownerId === userId || t.isOwner),
    [trips, userId],
  );

  const sharedTrips = useMemo(
    () => trips.filter((t) => t.ownerId !== userId && !t.isOwner),
    [trips, userId],
  );

  return {
    trips,
    ownedTrips,
    sharedTrips,
    tripLimitInfo,
    loading,
    error,
    refetch: fetchTrips,
    deleteTrip,
    createTrip,
    updateTripVisibility: async (tripId, isPublic) => {
      try {
        const response = await tripApi.updateVisibility(tripId, { isPublic });
        if (response.success) {
          setTrips((prev) =>
            prev.map((t) => (t.id === tripId ? { ...t, isPublic } : t)),
          );
          return true;
        }
        return false;
      } catch (err) {
        console.error("Failed to update trip visibility:", err);
        return false;
      }
    },
  };
};

export default useUserTrips;
