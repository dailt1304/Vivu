import React, {
  useState,
  useEffect,
  useRef,
  startTransition,
  useCallback,
} from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { addDays, parseISO, differenceInDays } from "date-fns";

import AppNavbar from "../../../components/layout/AppNavbar";
import { useAuth } from "../../../contexts/auth-context";
import tripApi from "../../../api/tripApi";
import toast from "../../../utils/toast";
import ConnectionError from "../../../components/common/ConnectionError";
import { Loader2 } from "lucide-react";
import { useCityById } from "../../../hooks/cities/useCities";

import TripDetailsDrawer from "../../../components/user/trips/TripDetailsDrawer/index.js";

// New extracted components
import TripChatPanel from "../../../components/user/trips/TripChatPanel";
import TripMapPanel from "../../../components/user/trips/TripMapPanel";
import TripHeaderToolbar from "../../../components/user/trips/TripHeaderToolbar";

const TripDetailPage = () => {
  const { id: routeId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [actualTripId, setActualTripId] = useState(routeId);

  useEffect(() => {
    setActualTripId(routeId);
  }, [routeId]);

  const id = actualTripId;

  const [tripData, setTripData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const [editableTitle, setEditableTitle] = useState("");
  const [editableStartDate, setEditableStartDate] = useState(new Date());
  const [editableEndDate, setEditableEndDate] = useState(
    addDays(new Date(), 2),
  );
  const [editableTripSize, setEditableTripSize] = useState(1);
  const [hoveredItemId, setHoveredItemId] = useState(null);
  const [roleUpdateCounter, setRoleUpdateCounter] = useState(0);

  const { user: currentUser, userId } = useAuth();
  const chatPanelRef = useRef();
  const tripUpdatedDebounceRef = useRef(null);
  const pendingTripUpdateRef = useRef(null);
  const { data: cityData } = useCityById(tripData?.cityId);

  const fetchTripById = useCallback(async (tripId) => {
    try {
      const response = await tripApi.getById(tripId);
      if (response.success && response.data) {
        setTripData(response.data);
      }
    } catch (err) {
      console.error("Failed to fetch saved trip:", err);
    }
  }, []);

  const fetchTrip = useCallback(
    async (showLoading = true) => {
      if (!id || id === "new") {
        if (id === "new") setLoading(false);
        return;
      }
      try {
        if (showLoading) setLoading(true);
        setError(null);
        const response = await tripApi.getById(id);
        if (response.success && response.data) {
          setTripData(response.data);
        } else {
          setError("Không tìm thấy chuyến đi");
        }
      } catch (err) {
        console.error("Failed to fetch trip:", err);
        setError("Không thể tải thông tin chuyến đi");
      } finally {
        if (showLoading) setLoading(false);
      }
    },
    [id],
  );

  const prevIdRef = useRef(id);
  useEffect(() => {
    const isTransitioningFromNew = prevIdRef.current === "new" && id !== "new";
    prevIdRef.current = id;
    fetchTrip(!isTransitioningFromNew);
  }, [fetchTrip, id]);

  // Broadcast to other members when returning from ExplorePage after adding alternative
  const broadcastNeededRef = useRef(false);
  if (location.state?.broadcastNeeded && !broadcastNeededRef.current) {
    broadcastNeededRef.current = true;
  }

  useEffect(() => {
    if (!broadcastNeededRef.current) return;
    // Clear navigation state so back button doesn't re-trigger
    navigate(location.pathname, { replace: true, state: {} });

    let attempts = 0;
    const maxAttempts = 20; // 20 * 500ms = 10 seconds max wait
    const interval = setInterval(() => {
      attempts++;
      const panel = chatPanelRef.current;
      // Must check isConnected — broadcastTripEdit always exists but silently
      // no-ops when SignalR isn't connected yet
      if (panel?.isConnected && panel?.broadcastTripEdit) {
        panel.broadcastTripEdit({ type: "manual_update" });
        broadcastNeededRef.current = false;
        clearInterval(interval);
      } else if (attempts >= maxAttempts) {
        broadcastNeededRef.current = false;
        clearInterval(interval);
      }
    }, 500);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleTripUpdated = useCallback(
    (payload) => {
      // Skip if this user triggered the change (avoid duplicate updates)
      if (payload?.changedBy && String(payload.changedBy) === String(userId)) {
        return;
      }

      // Debounce: collapse rapid consecutive events (e.g. swap = 2 API calls)
      // into a single toast + fetchTrip after 600ms quiet period.
      pendingTripUpdateRef.current = payload;
      if (tripUpdatedDebounceRef.current) {
        clearTimeout(tripUpdatedDebounceRef.current);
      }
      tripUpdatedDebounceRef.current = setTimeout(() => {
        const latestPayload = pendingTripUpdateRef.current;
        if (latestPayload?.type === "role_updated") {
          toast.info("Quyền hạn thành viên trong nhóm vừa được thay đổi!");
          setRoleUpdateCounter((prev) => prev + 1);
        } else {
          toast.info("Lịch trình vừa được cập nhật!");
        }
        fetchTrip(false);
        tripUpdatedDebounceRef.current = null;
        pendingTripUpdateRef.current = null;
      }, 600);
    },
    [fetchTrip, userId],
  );

  // Vercel Rule: rerender-memo — stable deps [fetchTrip, userId]
  const handleMemberListChanged = useCallback(
    (payload) => {
      // Refetch trip data (includes member count)
      fetchTrip(false);

      // Toast notification for the change
      const name = payload?.changedUserName || "Một thành viên";
      if (payload?.action === "joined") {
        toast.info(`${name} đã tham gia nhóm`);
      } else if (payload?.action === "left") {
        if (payload?.changedUserId !== userId) {
          toast.info(`${name} đã rời nhóm`);
        }
      } else if (payload?.action === "removed") {
        // Don't show this generic toast to the kicked user (they get MemberKicked instead)
        if (payload?.changedUserId !== userId) {
          toast.info(`${name} đã bị xóa khỏi nhóm`);
        }
      }
    },
    [fetchTrip, userId],
  );

  // Vercel Rule: rerender-memo — stable deps [userId, navigate]
  const handleMemberKicked = useCallback(
    (payload) => {
      // Only react if the kicked user is the current user
      if (payload?.kickedUserId === userId) {
        toast.error(
          `Bạn đã bị xóa khỏi chuyến đi "${payload?.tripTitle || "chuyến đi"}"`,
        );
        navigate("/my-trips");
      }
    },
    [userId, navigate],
  );

  useEffect(() => {
    if (tripData) {
      setEditableTitle(tripData.title || "Chuyến đi");
      setEditableStartDate(
        tripData.startDate ? parseISO(tripData.startDate) : new Date(),
      );
      setEditableEndDate(
        tripData.endDate ? parseISO(tripData.endDate) : addDays(new Date(), 2),
      );
      setEditableTripSize(tripData.tripSize || 1);
    }
  }, [tripData]);

  const handleUpdateField = useCallback(
    async (fieldName, value) => {
      if (
        !id ||
        !tripData ||
        value === null ||
        value === undefined ||
        value === ""
      )
        return;
      try {
        const payload = {
          title: tripData.title,
          startDate: tripData.startDate,
          endDate: tripData.endDate,
          tripSize: tripData.tripSize,
          cityId: tripData.cityId,
          description: tripData.description,
          [fieldName]: value,
        };
        if (fieldName === "startDate" || fieldName === "endDate") {
          payload[fieldName] = value.toISOString();
        }
        const response = await tripApi.update(id, payload);
        if (response.success) {
          setTripData((prev) => ({ ...prev, ...payload }));
          toast.success("Đã cập nhật!");
          chatPanelRef.current?.broadcastTripEdit?.({
            type: "field_update",
            fieldName,
            value,
          });
        }
      } catch (err) {
        console.error(`Failed to update ${fieldName}:`, err);
        toast.error("Lỗi khi cập nhật");
      }
    },
    [id, tripData],
  );

  const handleManualTripUpdate = useCallback((shouldBroadcast = true) => {
    if (shouldBroadcast) {
      chatPanelRef.current?.broadcastTripEdit?.({ type: "manual_update" });
    }
    fetchTrip(false);
  }, [fetchTrip]);

  const handleReorderSuccess = useCallback(() => {
    chatPanelRef.current?.broadcastTripEdit?.({ type: "reorder" });
  }, []);

  const handleRoleUpdateSuccess = useCallback(() => {
    chatPanelRef.current?.broadcastTripEdit?.({ type: "role_updated" });
    chatPanelRef.current?.refreshMembers?.();
    fetchTrip(false);
  }, [fetchTrip]);

  const handleTitleChange = useCallback(
    (newTitle) => setEditableTitle(newTitle),
    [],
  );
  const handleTitleSave = useCallback(() => {
    if (editableTitle && editableTitle.trim()) {
      handleUpdateField("title", editableTitle.trim());
    }
  }, [editableTitle, handleUpdateField]);

  const handleDateChange = useCallback(
    async (start, end) => {
      if (!id || !tripData || !start || !end) return;
      const oldDayCount = tripData?.tripDays?.length || 0;
      const difference = differenceInDays(end, start);
      if (difference <= 0) {
        toast.warning(
          "Chuyến đi phải kéo dài ít nhất 1 ngày (đi và về khác ngày).",
        );
        return;
      }
      const newTotalDays = difference + 1; // Matches backend: (end-start).Days + 1
      if (newTotalDays < oldDayCount) {
        // Backend will delete days where dayIndex > newTotalDays.
        // tripDays are sorted by dayIndex (1-based). We check those that will be removed.
        const excessDays = tripData.tripDays.filter(
          (day) => (day.dayIndex || 0) > newTotalDays && day.dayIndex > 0,
        );
        const daysWithLocations = excessDays.filter(
          (day) => day.locations && day.locations.length > 0,
        );
        if (daysWithLocations.length > 0) {
          toast.warning(
            `Không thể giảm ngày vì ${daysWithLocations.length} ngày cuối có địa điểm. Vui lòng xóa địa điểm trước.`,
          );
          return;
        }
      }
      setEditableStartDate(start);
      setEditableEndDate(end);
      try {
        const payload = {
          title: tripData.title,
          startDate: start.toISOString(),
          endDate: end.toISOString(),
          tripSize: tripData.tripSize,
          cityId: tripData.cityId,
          description: tripData.description,
        };
        const response = await tripApi.update(id, payload);
        if (response.success) {
          toast.success("Đã cập nhật thời gian!");
          chatPanelRef.current?.broadcastTripEdit?.({
            type: "date_update",
            start: start.toISOString(),
            end: end.toISOString(),
          });
          await fetchTrip(false);
        }
      } catch (err) {
        console.error("Failed to update dates:", err);
        toast.error("Lỗi khi cập nhật thời gian");
        if (tripData) {
          setEditableStartDate(
            tripData.startDate ? parseISO(tripData.startDate) : new Date(),
          );
          setEditableEndDate(
            tripData.endDate ? parseISO(tripData.endDate) : new Date(),
          );
        }
      }
    },
    [id, tripData, fetchTrip],
  );

  const handleTripSizeChange = useCallback(
    (value) => setEditableTripSize(value),
    [],
  );
  const handleTripSizeSave = useCallback(() => {
    if (editableTripSize && editableTripSize >= 1) {
      handleUpdateField("tripSize", editableTripSize);
    }
  }, [editableTripSize, handleUpdateField]);

  const handleVisibilityChange = useCallback(
    async (isPublic) => {
      if (!tripData || !id) return;
      const newVisibility = isPublic ? 1 : 0;
      const previousVisibility = tripData.visibility;
      startTransition(() => {
        setTripData((prev) => ({ ...prev, visibility: newVisibility }));
      });
      try {
        const response = await tripApi.updateVisibility(id, {
          visibility: newVisibility,
        });
        if (response.success || response.isSuccess) {
          toast.success("Đã cập nhật chế độ hiển thị!");
          chatPanelRef.current?.broadcastTripEdit?.({
            type: "visibility_update",
            visibility: newVisibility,
          });
        } else {
          throw new Error("Update visibility failed");
        }
      } catch (err) {
        console.error("Failed to update visibility:", err);
        toast.error("Không thể thay đổi chế độ hiển thị");
        startTransition(() => {
          setTripData((prev) => ({ ...prev, visibility: previousVisibility }));
        });
      }
    },
    [id, tripData],
  );

  const handleDrawerClose = useCallback(() => setIsDrawerOpen(false), []);
  const handleItemHover = useCallback((itemId) => setHoveredItemId(itemId), []);
  const handleItemLeave = useCallback(() => setHoveredItemId(null), []);

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

  if (error) {
    return (
      <div className="h-screen flex flex-col bg-white">
        <AppNavbar />
        <ConnectionError
          message={error}
          onRetry={fetchTrip}
          isRetrying={loading}
        />
      </div>
    );
  }

  return (
    <div className="h-screen flex flex-col overflow-hidden bg-white">
      <AppNavbar />

      <TripHeaderToolbar
        tripData={tripData}
        cityData={cityData}
        startDate={editableStartDate}
        endDate={editableEndDate}
        id={id}
        isDrawerOpen={isDrawerOpen}
        setIsDrawerOpen={setIsDrawerOpen}
      />

      <div className="flex-1 flex flex-col md:flex-row overflow-hidden">
        <TripChatPanel
          ref={chatPanelRef}
          id={id}
          actualTripId={actualTripId}
          setActualTripId={setActualTripId}
          tripData={tripData}
          currentUser={currentUser}
          userId={userId}
          fetchTrip={fetchTrip}
          fetchTripById={fetchTripById}
          setTripData={setTripData}
          setEditableTitle={setEditableTitle}
          setEditableStartDate={setEditableStartDate}
          setEditableEndDate={setEditableEndDate}
          setIsDrawerOpen={setIsDrawerOpen}
          handleTripUpdated={handleTripUpdated}
          handleMemberListChanged={handleMemberListChanged}
          handleMemberKicked={handleMemberKicked}
        />

        <TripMapPanel tripData={tripData} hoveredItemId={hoveredItemId} />
      </div>

      <TripDetailsDrawer
        isOpen={isDrawerOpen}
        onClose={handleDrawerClose}
        tripData={tripData}
        tripTitle={editableTitle}
        startDate={editableStartDate}
        endDate={editableEndDate}
        tripSize={editableTripSize}
        onTitleChange={handleTitleChange}
        onTitleSave={handleTitleSave}
        onDateChange={handleDateChange}
        onTripSizeChange={handleTripSizeChange}
        onTripSizeSave={handleTripSizeSave}
        onVisibilityChange={handleVisibilityChange}
        onUpdateField={handleUpdateField}
        onItemHover={handleItemHover}
        onItemLeave={handleItemLeave}
        onTripUpdate={handleManualTripUpdate}
        onReorderSuccess={handleReorderSuccess}
        roleUpdateCounter={roleUpdateCounter}
        onRoleUpdateSuccess={handleRoleUpdateSuccess}
        destination={cityData?.name}
        currentUser={currentUser}
        userId={userId}
      />
    </div>
  );
};

export default TripDetailPage;
