import { useState, useEffect, useCallback } from "react";
import { parseISO, addDays, differenceInDays } from "date-fns";
import tripApi from "../../api/tripApi";
import tripDayApi from "../../api/tripDayApi";
import toast from "../../utils/toast";

/**
 * Custom hook for fetching and managing a single trip's details
 *
 * @param {string} tripId - The trip ID to fetch
 * @returns {{
 *   trip: object | null,
 *   loading: boolean,
 *   error: string | null,
 *   refetch: (showLoading?: boolean) => Promise<void>,
 *   updateField: (fieldName: string, value: any) => Promise<boolean>,
 *   updateDates: (start: Date, end: Date) => Promise<boolean>,
 *   editableTitle: string,
 *   setEditableTitle: Function,
 *   editableStartDate: Date,
 *   editableEndDate: Date,
 *   editableTripSize: number,
 *   setEditableTripSize: Function,
 *   saveTitle: () => Promise<void>,
 *   saveTripSize: () => Promise<void>
 * }}
 */
const useTripDetails = (tripId) => {
  const [trip, setTrip] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Editable states
  const [editableTitle, setEditableTitle] = useState("");
  const [editableStartDate, setEditableStartDate] = useState(new Date());
  const [editableEndDate, setEditableEndDate] = useState(new Date());
  const [editableTripSize, setEditableTripSize] = useState(1);

  // Fetch trip data
  const fetchTrip = useCallback(
    async (showLoading = true) => {
      if (!tripId) return;

      try {
        if (showLoading) {
          setLoading(true);
        }
        setError(null);

        const response = await tripApi.getById(tripId);

        if (response.success && response.data) {
          setTrip(response.data);
        } else {
          setError("Không tìm thấy chuyến đi");
        }
      } catch (err) {
        console.error("Failed to fetch trip:", err);
        setError("Không thể tải thông tin chuyến đi");
      } finally {
        if (showLoading) {
          setLoading(false);
        }
      }
    },
    [tripId],
  );

  // Initial fetch
  useEffect(() => {
    fetchTrip();
  }, [fetchTrip]);

  // Sync editable states when trip data loads
  useEffect(() => {
    if (trip) {
      setEditableTitle(trip.title || "Chuyến đi");
      setEditableStartDate(
        trip.startDate ? parseISO(trip.startDate) : new Date(),
      );
      setEditableEndDate(
        trip.endDate ? parseISO(trip.endDate) : addDays(new Date(), 2),
      );
      setEditableTripSize(trip.tripSize || 1);
    }
  }, [trip]);

  // Update a single field
  const updateField = useCallback(
    async (fieldName, value) => {
      if (
        !tripId ||
        !trip ||
        value === null ||
        value === undefined ||
        value === ""
      ) {
        return false;
      }

      try {
        const payload = {
          title: trip.title,
          startDate: trip.startDate,
          endDate: trip.endDate,
          tripSize: trip.tripSize,
          destination: trip.destination,
          description: trip.description,
          [fieldName]: value,
        };

        // Convert dates to ISO string
        if (fieldName === "startDate" || fieldName === "endDate") {
          payload[fieldName] = value.toISOString();
        }

        const response = await tripApi.update(tripId, payload);

        if (response.success) {
          setTrip((prev) => ({ ...prev, ...payload }));
          toast.success("Đã cập nhật!");
          return true;
        }
        return false;
      } catch (err) {
        console.error(`Failed to update ${fieldName}:`, err);
        toast.error("Lỗi khi cập nhật");
        return false;
      }
    },
    [tripId, trip],
  );

  // Update dates with TripDay synchronization
  const updateDates = useCallback(
    async (start, end) => {
      if (!tripId || !trip || !start || !end) return false;

      const difference = differenceInDays(end, start);
      if (difference <= 0) {
        toast.warning("Chuyến đi phải kéo dài ít nhất 1 ngày (đi và về khác ngày).");
        return false;
      }

      const oldDayCount = trip?.tripDays?.length || 0;
      const newDayCount = difference;

      // Check if reducing days and excess days have locations
      if (newDayCount < oldDayCount) {
        const excessDays = trip.tripDays.slice(newDayCount);
        const daysWithLocations = excessDays.filter(
          (day) => day.locations && day.locations.length > 0,
        );

        if (daysWithLocations.length > 0) {
          toast.warning(
            `Không thể giảm ngày vì ${daysWithLocations.length} ngày cuối có địa điểm. Vui lòng xóa địa điểm trước.`,
          );
          return false;
        }
      }

      setEditableStartDate(start);
      setEditableEndDate(end);

      try {
        // 1. Update trip metadata
        const payload = {
          title: trip.title,
          startDate: start.toISOString(),
          endDate: end.toISOString(),
          tripSize: trip.tripSize,
          destination: trip.destination,
          description: trip.description,
        };

        const response = await tripApi.update(tripId, payload);

        if (response.success) {
          // 2. Sync TripDays
          if (newDayCount > oldDayCount) {
            for (let i = oldDayCount; i < newDayCount; i++) {
              const dayDate = addDays(start, i);
              await tripDayApi.add({
                tripId: tripId,
                dayDate: dayDate.toISOString(),
              });
            }
            toast.success(`Đã thêm ${newDayCount - oldDayCount} ngày mới!`);
          } else if (newDayCount < oldDayCount) {
            const excessDays = trip.tripDays.slice(newDayCount);
            for (const day of excessDays) {
              await tripDayApi.remove(day.id);
            }
            toast.success(`Đã xóa ${oldDayCount - newDayCount} ngày!`);
          } else {
            toast.success("Đã cập nhật thời gian!");
          }

          // 3. Refresh trip data silently
          await fetchTrip(false);
          return true;
        }
        return false;
      } catch (err) {
        console.error("Failed to update dates:", err);
        toast.error("Lỗi khi cập nhật thời gian");

        // Revert on error
        if (trip) {
          setEditableStartDate(
            trip.startDate ? parseISO(trip.startDate) : new Date(),
          );
          setEditableEndDate(
            trip.endDate ? parseISO(trip.endDate) : new Date(),
          );
        }
        return false;
      }
    },
    [tripId, trip, fetchTrip],
  );

  // Save title helper
  const saveTitle = useCallback(async () => {
    if (editableTitle && editableTitle.trim()) {
      await updateField("title", editableTitle.trim());
    }
  }, [editableTitle, updateField]);

  // Save trip size helper
  const saveTripSize = useCallback(async () => {
    if (editableTripSize && editableTripSize >= 1) {
      await updateField("tripSize", editableTripSize);
    }
  }, [editableTripSize, updateField]);

  return {
    trip,
    loading,
    error,
    refetch: fetchTrip,
    updateField,
    updateDates,
    editableTitle,
    setEditableTitle,
    editableStartDate,
    editableEndDate,
    editableTripSize,
    setEditableTripSize,
    saveTitle,
    saveTripSize,
  };
};

export default useTripDetails;
