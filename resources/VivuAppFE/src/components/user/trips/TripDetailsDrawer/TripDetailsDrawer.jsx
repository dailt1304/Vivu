import React, { useState, useEffect, useRef } from "react";
import PropTypes from "prop-types";
import { useNavigate } from "react-router-dom";


import { Image, List, MessageCircle } from "lucide-react";
import { format, differenceInDays } from "date-fns";
import toast from "../../../../utils/toast";
import tripLocationApi from "../../../../api/tripLocationApi";

import tripMemberApi from "../../../../api/tripMemberApi";
import { getSmartDropTime, calculateEndTime } from "../TripItinerary/utils/dragTimeCalculator";
// TripItinerary compound component (Vercel Composition Pattern)
import { usePermissions } from "./usePermissions";
import TripTabContent from "./TripTabContent";
import TripModals from "./TripModals";
import TripDrawerTopBar from "./TripDrawerTopBar";
import TripInfoHeader from "./TripInfoHeader";
import TripDrawerTabs from "./TripDrawerTabs";
import { useIdeasManagement } from "./useIdeasManagement";

// Feature flag removed - fully migrated to TripItinerary compound component
const START_HOUR = 6;
const END_HOUR = 23;
const TOTAL_DAY_MINUTES = (END_HOUR - START_HOUR) * 60;

// Helper to parse JSON images or return fallback
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

const TripDetailsDrawer = ({
  isOpen,
  onClose,
  tripTitle,
  onTitleChange,
  onTitleSave,
  startDate,
  endDate,
  onDateChange,
  tripData,
  tripSize,
  onTripSizeChange,
  onTripSizeSave,
  onItemHover,
  onItemLeave,
  currentUser,
  userId,
  onFlyTo,
  onVisibilityChange,
  onReorderSuccess,
  onRoleUpdateSuccess,
  onTripUpdate,
  roleUpdateCounter,
  destination,
}) => {
  const navigateToExplore = useNavigate();
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [members, setMembers] = useState([]);
  const [isPublic, setIsPublic] = useState(
    tripData?.visibility === "Public" || tripData?.visibility === 1,
  );

  // Permission Logic
  const { canEdit, canModifyMembers } = usePermissions(
    tripData,
    currentUser,
    userId,
    members,
  );

  useEffect(() => {
    // eslint-disable-next-line
    setIsPublic(
      tripData?.visibility === "Public" || tripData?.visibility === 1,
    );
  }, [tripData?.visibility]);

  const handleToggleVisibility = () => {
    const newState = !isPublic;
    onVisibilityChange?.(newState);
  };

  // Fetch members logic lifted from Tab
  const fetchMembers = React.useCallback(async () => {
    const tId = tripData?.id || tripData?.Id;
    if (!tId) return;
    try {
      const res = await tripMemberApi.getMembers(tId);
      if (res.success || res.isSuccess) {
        const items = res.data?.items || res.data || res.value?.items || [];
        setMembers(Array.isArray(items) ? items : []);
      }
    } catch (error) {
      console.error("Failed to fetch members in drawer", error);
    }
  }, [tripData]);

  useEffect(() => {
    if (tripData?.id || tripData?.Id) {
      // eslint-disable-next-line
      fetchMembers();
    }
  }, [tripData, fetchMembers]);

  useEffect(() => {
    if (roleUpdateCounter > 0) {
      void fetchMembers();
    }
  }, [roleUpdateCounter, fetchMembers]);

  const [activeTab, setActiveTab] = useState("itinerary");
  const [activeMenuId, setActiveMenuId] = useState(null);
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [isLocationDrawerOpen, setIsLocationDrawerOpen] = useState(false);
  const [isEditDateOpen, setIsEditDateOpen] = useState(false);

  // Edit Time Modal State
  const [isEditTimeModalOpen, setIsEditTimeModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState(null);

  const [deleteConfirmation, setDeleteConfirmation] = useState({
    isOpen: false,
    itemId: null,
    containerId: null,
  });

  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
  const [selectedIdeaId, setSelectedIdeaId] = useState(null);

  // Swap Modal State
  const [isSwapModalOpen, setIsSwapModalOpen] = useState(false);
  const [selectedSwapIdeaId, setSelectedSwapIdeaId] = useState(null);

  // Initial Data State
  const [items, setItems] = useState({
    ideas: [],
    "day-1": [],
    "day-2": [],
    "day-3": [],
  });

  // Populate items from tripData when it changes
  // Also store tripDayId mapping for API calls
  const [tripDayIdMap, setTripDayIdMap] = useState({});

  // Use ref to access current items without adding to dependencies
  const itemsRef = useRef(items);

  // Update ref in effect to avoid updating during render
  useEffect(() => {
    itemsRef.current = items;
  }, [items]);

  useEffect(() => {
    if (tripData?.tripDays) {
      // Filter out ideas day (DayIndex: 0)
      const ideasDay = tripData.tripDays.find((d) => d.dayIndex === 0);
      const regularDays = tripData.tripDays
        .filter((d) => d.dayIndex !== 0)
        .sort((a, b) => a.dayIndex - b.dayIndex);

      const dayIdMap = {};
      if (ideasDay) {
        dayIdMap["ideas"] = ideasDay.id;
      }

      const newItems = {
        ideas: ideasDay
          ? (ideasDay.locations || []).map((loc, locIdx) => {
              const parsedImgs = parseImages(
                loc.location?.locationDetail?.images,
              );
              return {
                id: `idea-persisted-${loc.id}`,
                tripLocationId: loc.id,
                content: loc.location?.name || `Gợi ý ${locIdx + 1}`,
                type: "idea",
                category: loc.location?.category || "Attraction",
            image:
              (parsedImgs[0]?.url || parsedImgs[0]) ||
              loc.location?.imageUrl ||
              "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop",
                latitude: loc.location?.latitude,
                longitude: loc.location?.longitude,
                locationId: loc.locationId || loc.location?.id,
                orderIndex: loc.orderIndex || locIdx,
              };
            })
          : [],
      };

      regularDays.forEach((day, dayIdx) => {
        const dayKey = `day-${dayIdx + 1}`;
        dayIdMap[dayKey] = day.id;

        newItems[dayKey] = (day.locations || []).map((loc, locIdx) => {
          const parsedImgs = parseImages(loc.location?.locationDetail?.images);
          return {
            id: `item-${day.dayIndex || dayIdx + 1}-${locIdx}`,
            tripLocationId: loc.id,
            content: loc.location?.name || `Địa điểm ${locIdx + 1}`,
            type: "item",
            category: loc.location?.category || "Attraction",
            image:
              (parsedImgs[0]?.url || parsedImgs[0]) ||
              loc.location?.imageUrl ||
              "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop",
            latitude: loc.location?.latitude,
            longitude: loc.location?.longitude,
            startTime: loc.startTime?.slice(0, 5) || "09:00",
            endTime: loc.endTime?.slice(0, 5) || "11:00",
            note: loc.note || loc.location?.description || "",
            locationId: loc.locationId || loc.location?.id,
            orderIndex: loc.orderIndex || locIdx,
            alternatives: loc.alternatives || [],
          };
        });
      });

      setItems(newItems);
      setTripDayIdMap(dayIdMap);
    }
  }, [tripData]);

  // Ensure items has keys for all days (Dynamic State Handling is tricky in simple useState.
  // Ideally we update items when dayIds change, but basic read access needs safety).

  // Dynamic Date Logic
  const tripDurationDays = differenceInDays(endDate, startDate);
  const dayIds = Array.from(
    { length: Math.max(1, tripDurationDays) },
    (_, i) => `day-${i + 1}`,
  );

  // Sync items state with dayIds
  // useEffect(() => { // Removed useEffect as per instruction
  //     setItems(prev => {
  //         const newItems = { ...prev };
  //         let hasChanges = false;
  //         dayIds.forEach(dayId => {
  //             if (!newItems[dayId]) {
  //                 newItems[dayId] = [];
  //                 hasChanges = true;
  //             }
  //         });
  //         return hasChanges ? newItems : prev;
  //     });
  // }, [dayIds.length]); // Only when number of days changes

  const getDateLabel = React.useCallback((dayIndex) => {
    const date = new Date(startDate);
    date.setDate(startDate.getDate() + dayIndex);
    return format(date, "dd/MM/yyyy");
  }, [startDate]);

  const dayOptions = React.useMemo(() =>
    dayIds.map((id, index) => ({
      id,
      label: `Ngày ${index + 1} - ${getDateLabel(index)}`,
    })),
    [dayIds, getDateLabel]
  );

  const handleToggleMenu = React.useCallback((id) => {
    setActiveMenuId((prev) => (prev === id ? null : id));
  }, []);
  const handleTimeChange = async (id, field, value) => {
    let itemToUpdate = null;
    let dayKeyFound = null;

    setItems((prev) => {
      const newItems = { ...prev };
      const dayKey = Object.keys(newItems).find(
        (k) => k.startsWith("day-") && newItems[k].some((i) => i.id === id),
      );

      if (dayKey) {
        const index = newItems[dayKey].findIndex((i) => i.id === id);
        if (index !== -1) {
          const currentItem = newItems[dayKey][index];
          itemToUpdate = { ...currentItem, [field]: value };
          dayKeyFound = dayKey;

          const updatedList = [...newItems[dayKey]];
          updatedList[index] = itemToUpdate;
          newItems[dayKey] = updatedList;
        }
      }
      return newItems;
    });

    // API Call & Sync
    if (
      itemToUpdate &&
      itemToUpdate.tripLocationId &&
      dayKeyFound &&
      dayKeyFound !== "ideas"
    ) {
      try {
        const tripDayId = tripDayIdMap[dayKeyFound];
        const index = items[dayKeyFound].findIndex((i) => i.id === id);

        await tripLocationApi.update(itemToUpdate.tripLocationId, {
          tripDayId,
          locationId: itemToUpdate.locationId,
          orderIndex: index,
          startTime:
            field === "startTime"
              ? value + ":00"
              : itemToUpdate.startTime + ":00",
          endTime:
            field === "endTime" ? value + ":00" : itemToUpdate.endTime + ":00",
          note: itemToUpdate.note,
          transportMode: itemToUpdate.transportMode,
        });

        onTripUpdate?.(false);
      } catch (err) {
        console.error("Failed to update time inline:", err);
        toast.error("Không thể cập nhật thời gian");
      }
    }
  };

  const handleNoteChange = React.useCallback(
    async (itemId, noteText) => {
      let foundItem = null;
      let foundDayKey = null;

      const currentItems = itemsRef.current;
      for (const [key, dayItems] of Object.entries(currentItems)) {
        if (!key.startsWith("day-")) continue;
        const item = dayItems.find((i) => i.id === itemId);
        if (item) {
          foundItem = item;
          foundDayKey = key;
          break;
        }
      }

      if (!foundItem?.tripLocationId || !foundDayKey) return;

      setItems((prev) => ({
        ...prev,
        [foundDayKey]: prev[foundDayKey].map((i) =>
          i.id === itemId ? { ...i, note: noteText } : i
        ),
      }));

      try {
        const tripDayId = tripDayIdMap[foundDayKey];
        await tripLocationApi.update(foundItem.tripLocationId, {
          tripDayId,
          locationId: foundItem.locationId,
          orderIndex: foundItem.orderIndex,
          startTime: foundItem.startTime ? foundItem.startTime + ":00" : null,
          endTime: foundItem.endTime ? foundItem.endTime + ":00" : null,
          note: noteText,
          transportMode: foundItem.transportMode || null,
        });
      } catch (err) {
        console.error("Failed to save note:", err);
        toast.error("Không thể lưu ghi chú");
      }
    },
    [tripDayIdMap]
  );

  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  const { handleConfirmAddLocation } = useIdeasManagement({
    tripId: tripData?.id || tripData?.Id,
    items,
    setItems,
    tripDayIdMap,
    setTripDayIdMap,
    onFlyTo,
    canEdit,
    onTripUpdate,
  });

  const requestDelete = (id, containerId) => {
    setDeleteConfirmation({
      isOpen: true,
      itemId: id,
      containerId: containerId,
    });
  };

  const handleConfirmDelete = async () => {
    const { itemId, containerId } = deleteConfirmation;
    if (!itemId || !containerId) return;

    // Find item details
    const itemToDelete = items[containerId]?.find((i) => i.id === itemId);
    if (!itemToDelete) {
      setDeleteConfirmation({ isOpen: false, itemId: null, containerId: null });
      return;
    }

    // Optimistic UI Update
    const prevItems = { ...items };
    setItems((prev) => ({
      ...prev,
      [containerId]: prev[containerId].filter((i) => i.id !== itemId),
    }));
    setDeleteConfirmation({ isOpen: false, itemId: null, containerId: null });

    // If item has tripLocationId (persisted), call API
    if (itemToDelete.tripLocationId) {
      try {
        const response = await tripLocationApi.remove(
          itemToDelete.tripLocationId,
        );
        const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
        if (isApiSuccess) {
          toast.success(`Đã xóa "${itemToDelete.content}" khỏi lịch trình`);
          onTripUpdate?.(false);
        } else {
          // Revert on failure
          setItems(prevItems);
          toast.error("Không thể xóa địa điểm");
        }
      } catch (error) {
        console.error("Delete failed:", error);
        // Revert on error
        setItems(prevItems);
        toast.error("Lỗi khi xóa địa điểm");
      }
    } else {
      // Local item (Ideas), just toast
      toast.success(`Đã xóa "${itemToDelete.content}" khỏi danh sách`);
    }
  };

  const handleAddToSchedule = (id) => {
    setSelectedIdeaId(id);
    setIsScheduleModalOpen(true);
  };

  const handleQuickAddToDay = async (ideaId, dayId) => {
    const ideaItem = items.ideas.find((i) => i.id === ideaId);
    if (!ideaItem) return;

    const tripDayId = tripDayIdMap[dayId];
    if (!tripDayId) return;

    // Auto calculate time
    const dayItems = items[dayId] || [];
    const orderIndex = dayItems.length;
    const startTime = getSmartDropTime(null, dayId, orderIndex, dayItems);
    const endTime = calculateEndTime(startTime);

    // Temp item for optimistic UI
    const tempId = `item-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const newItem = {
      ...ideaItem,
      id: tempId,
      type: "item",
      startTime,
      endTime,
      orderIndex,
    };

    // Optimistic UI
    const prevItems = { ...items };
    setItems((prev) => ({
      ...prev,
      ideas: prev.ideas.filter((i) => i.id !== ideaId),
      [dayId]: [...(prev[dayId] || []), newItem],
    }));

    // API Call
    try {
      let response;
      if (ideaItem.tripLocationId) {
        response = await tripLocationApi.update(ideaItem.tripLocationId, {
          tripDayId,
          locationId: ideaItem.locationId,
          orderIndex,
          startTime: startTime + ":00",
          endTime: endTime + ":00",
          note: ideaItem.note || null,
          transportMode: ideaItem.transportMode || null,
        });
      } else {
        response = await tripLocationApi.add({
          tripDayId,
          locationId: ideaItem.locationId,
          orderIndex,
          startTime: startTime + ":00",
          endTime: endTime + ":00",
          note: null,
          transportMode: null,
        });
      }

      const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
      const data = response?.value || response?.data || response;

      if (isApiSuccess && data) {
        setItems((prev) => ({
          ...prev,
          [dayId]: prev[dayId].map((item) =>
            item.id === tempId
              ? { ...item, tripLocationId: data.id || data.tripLocationId }
              : item
          ),
        }));
        toast.success(`Đã thêm "${ideaItem.content}" vào lịch trình`);
        onTripUpdate?.(false);
      } else {
        setItems(prevItems);
        toast.error("Không thể thêm địa điểm vào lịch trình");
      }
    } catch (err) {
      console.error("Quick add to day failed:", err);
      setItems(prevItems);
      toast.error("Lỗi khi thêm địa điểm");
    }

    if (ideaItem.latitude && ideaItem.longitude) {
      onFlyTo?.({
        latitude: ideaItem.latitude,
        longitude: ideaItem.longitude,
        zoom: 14,
      });
    }
  };

  const handleSwapLocation = (ideaId) => {
    setSelectedSwapIdeaId(ideaId);
    setIsSwapModalOpen(true);
  };

  const handleConfirmSwap = async (targetItem, targetDayId) => {
    const ideaItem = items.ideas.find((i) => i.id === selectedSwapIdeaId);
    if (!ideaItem || !targetItem) return;

    const tripDayId = tripDayIdMap[targetDayId];
    const ideasDayId = tripDayIdMap["ideas"];
    if (!tripDayId || !ideasDayId) return;

    const prevItems = { ...items };

    // Optimistic UI update — true swap: idea → schedule, target → ideas
    setItems((prev) => {
      // Ý tưởng lên lịch trình (thế chỗ target, giữ giờ)
      const newIdeaAsScheduled = {
        ...ideaItem,
        id: `item-swap-${Date.now()}`,
        type: "item",
        startTime: targetItem.startTime,
        endTime: targetItem.endTime,
      };

      // Địa điểm lịch trình xuống ý tưởng (xóa giờ)
      const newTargetAsIdea = {
        ...targetItem,
        id: `idea-persisted-${targetItem.tripLocationId}`,
        type: "idea",
        startTime: null,
        endTime: null,
      };

      const newDayList = prev[targetDayId].map((i) =>
        i.id === targetItem.id ? newIdeaAsScheduled : i
      );

      return {
        ...prev,
        ideas: [
          newTargetAsIdea,
          ...prev.ideas.filter((i) => i.id !== selectedSwapIdeaId),
        ],
        [targetDayId]: newDayList,
      };
    });

    setIsSwapModalOpen(false);
    setSelectedSwapIdeaId(null);

    try {
      // Step 1: Di chuyển ý tưởng lên lịch trình (vào vị trí + giờ của target)
      if (ideaItem.tripLocationId) {
        await tripLocationApi.update(ideaItem.tripLocationId, {
          tripDayId,
          locationId: ideaItem.locationId,
          orderIndex: targetItem.orderIndex || 0,
          startTime: targetItem.startTime ? targetItem.startTime + ":00" : null,
          endTime: targetItem.endTime ? targetItem.endTime + ":00" : null,
          note: ideaItem.note || null,
          transportMode: ideaItem.transportMode || null,
        });
      }

      // Step 2: Di chuyển địa điểm lịch trình xuống ý tưởng (Day 0, xóa giờ)
      if (targetItem.tripLocationId) {
        await tripLocationApi.update(targetItem.tripLocationId, {
          tripDayId: ideasDayId,
          locationId: targetItem.locationId,
          orderIndex: 0,
          startTime: null,
          endTime: null,
          note: targetItem.note || null,
          transportMode: targetItem.transportMode || null,
        });
      }

      toast.success(
        `Đã hoán đổi "${ideaItem.content}" và "${targetItem.content}"`
      );
      onTripUpdate?.(false);
    } catch (err) {
      console.error("Swap failed:", err);
      // Revert optimistic UI on error
      setItems(prevItems);
      toast.error("Không thể hoán đổi địa điểm, vui lòng thử lại.");
    }
  };

  const handleMoveToIdeas = async (item) => {
    if (!item.tripLocationId) return;

    const ideasDayId = tripDayIdMap["ideas"];
    if (!ideasDayId) {
      toast.error("Không tìm thấy khu vực ý tưởng");
      return;
    }

    const prevItems = { ...items };

    // Find where the item is currently
    const containerId = Object.keys(items).find((key) =>
      items[key].find((i) => i.id === item.id),
    );

    if (!containerId || containerId === "ideas") return;

    // Optimistic UI Update
    setItems((prev) => {
      const removedItem = prev[containerId].find((i) => i.id === item.id);
      if (!removedItem) return prev;

      const newItem = {
        ...removedItem,
        id: `idea-persisted-${removedItem.tripLocationId}`,
        type: "idea",
        startTime: null,
        endTime: null,
      };

      return {
        ...prev,
        [containerId]: prev[containerId].filter((i) => i.id !== item.id),
        ideas: [newItem, ...prev.ideas],
      };
    });

    try {
      const response = await tripLocationApi.update(item.tripLocationId, {
        tripDayId: ideasDayId,
        locationId: item.locationId,
        orderIndex: 0,
        startTime: null,
        endTime: null,
        note: item.note || null,
        transportMode: item.transportMode || null,
      });

      const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
      if (isApiSuccess) {
        toast.success(`Đã đưa "${item.content}" vào phần ý tưởng`);
        onTripUpdate?.(false);
      } else {
        setItems(prevItems);
        toast.error("Không thể di chuyển địa điểm");
      }
    } catch (err) {
      console.error("Move to ideas failed:", err);
      setItems(prevItems);
      toast.error("Lỗi khi di chuyển địa điểm");
    }
  };

  const handleEditTime = (item) => {
    setEditingItem(item);
    setIsEditTimeModalOpen(true);
  };

  const handleConfirmEditTime = (startTime, endTime) => {
    if (!editingItem) return;

    // Find container of editingItem for API context
    const containerId = Object.keys(items).find((key) =>
      items[key].find((i) => i.id === editingItem.id),
    );
    const prevItems = { ...items };

    // Optimistic Update
    setItems((prev) => {
      const newItems = { ...prev };
      if (containerId) {
        const list = [...newItems[containerId]];
        const index = list.findIndex((i) => i.id === editingItem.id);
        if (index !== -1) {
          list[index] = { ...list[index], startTime, endTime };
          // Re-sort the whole list
          newItems[containerId] = list.sort((a, b) => {
            if (!a.startTime) return 1;
            if (!b.startTime) return -1;
            return a.startTime.localeCompare(b.startTime);
          });
        }
      }
      return newItems;
    });

    // API Call
    if (containerId && containerId !== "ideas") {
      const tripDayId = tripDayIdMap[containerId];
      // Note: orderIndex might be needed. We use current index.
      const currentList = items[containerId];
      const index = currentList.findIndex((i) => i.id === editingItem.id);

      if (tripDayId && editingItem.tripLocationId) {
        (async () => {
          try {
            await tripLocationApi.update(editingItem.tripLocationId, {
              tripDayId: tripDayId,
              locationId: editingItem.locationId,
              orderIndex: index,
              startTime: startTime + ":00", // Ensure seconds
              endTime: endTime + ":00",
              note: editingItem.note,
              transportMode: editingItem.transportMode,
            });
            toast.success(`Đã cập nhật thời gian cho "${editingItem.content}"`);
            onTripUpdate?.(false);
          } catch (err) {
            console.error("Failed to update time:", err);
            setItems(prevItems);
            toast.error("Không thể cập nhật thời gian");
          }
        })();
      } else {
        // Fallback/Local success message
        toast.success(`Đã cập nhật thời gian cho "${editingItem.content}"`);
      }
    } else {
      toast.success(`Đã cập nhật thời gian cho "${editingItem.content}"`);
    }

    setIsEditTimeModalOpen(false);
    setEditingItem(null);
  };

  const handleConfirmSchedule = (dayId, startTime, endTime) => {
    const ideaItem = items.ideas.find((i) => i.id === selectedIdeaId);
    if (!ideaItem) return;

    const tempId = `item-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const newItem = {
      ...ideaItem,
      id: tempId,
      type: "item",
      startTime,
      endTime,
    };

    // Optimistic UI Update
    const prevItems = { ...items };
    setItems((prev) => {
      const dayList = [...(prev[dayId] || [])];
      // Insert at correct position based on startTime
      const insertIndex = dayList.findIndex((item) => {
        if (!item.startTime) return true; // Items without time go to end
        return startTime.localeCompare(item.startTime) < 0;
      });

      if (insertIndex === -1) {
        dayList.push(newItem);
      } else {
        dayList.splice(insertIndex, 0, newItem);
      }

      return {
        ...prev,
        ideas: prev.ideas.filter((i) => i.id !== selectedIdeaId),
        [dayId]: dayList,
      };
    });

    // API Call
    const tripDayId = tripDayIdMap[dayId];
    if (tripDayId && ideaItem.locationId) {
      (async () => {
        try {
          const currentList = items[dayId] || [];
          const orderIndex = currentList.length;

          let response;
          if (ideaItem.tripLocationId) {
            // Already persisted in Ideas (Day 0), so MOVE it by updating TripDayId
            response = await tripLocationApi.update(ideaItem.tripLocationId, {
              tripDayId: tripDayId,
              locationId: ideaItem.locationId,
              orderIndex: orderIndex,
              startTime: startTime + ":00",
              endTime: endTime + ":00",
              note: ideaItem.note || null,
              transportMode: ideaItem.transportMode || null,
            });
          } else {
            // New item, add it
            response = await tripLocationApi.add({
              tripDayId: tripDayId,
              locationId: ideaItem.locationId,
              orderIndex: orderIndex,
              startTime: startTime + ":00",
              endTime: endTime + ":00",
              note: null,
              transportMode: null,
            });
          }

          const isApiSuccess = !response || (response.isSuccess !== false && response.success !== false);
          const data = response?.value || response?.data || response;

          if (isApiSuccess && data) {
            // Update item with real tripLocationId
            setItems((prev) => ({
              ...prev,
              [dayId]: prev[dayId].map((item) =>
                item.id === tempId
                  ? { ...item, tripLocationId: data.id || data.tripLocationId }
                  : item,
              ),
            }));
            toast.success(
              `Đã thêm "${ideaItem.content || "địa điểm"}" vào lịch trình thành công`,
            );
            onTripUpdate?.(false);
          } else {
            // Revert on failure
            setItems(prevItems);
            toast.error("Không thể thêm địa điểm vào lịch trình");
          }
        } catch (err) {
          console.error("Failed to add scheduled item:", err);
          setItems(prevItems);
          toast.error("Lỗi khi thêm địa điểm");
        }
      })();
    } else {
      // Fallback if no API context (should not happen in real usage)
      toast.success(
        `Đã thêm "${ideaItem.content || "địa điểm"}" vào lịch trình thành công`,
      );
    }

    // Fly to location
    if (ideaItem.latitude && ideaItem.longitude) {
      onFlyTo?.({
        latitude: ideaItem.latitude,
        longitude: ideaItem.longitude,
        zoom: 14,
      });
    }

    setIsScheduleModalOpen(false);
    setSelectedIdeaId(null);
  };

  const [isExpanded, setIsExpanded] = useState(false); // State for drawer expansion (left vs right)

  // System Events for Plan B Alternatives
  useEffect(() => {
    const handlePickAlt = (e) => {
      const { item } = e.detail;
      const tripId = tripData?.id || tripData?.Id;
      const cId = tripData?.cityId || "";
      const cName = destination || "";
      const catId = typeof item.category === "object" ? item.category?.id : "";
      navigateToExplore(`/explore?mode=pick-alternative&tripLocationId=${item.tripLocationId}&tripId=${tripId}&refLoc=${encodeURIComponent(item.content)}&lat=${item.latitude || ""}&lng=${item.longitude || ""}${cId ? `&cityId=${cId}&cityName=${encodeURIComponent(cName)}` : ""}${catId ? `&categoryId=${catId}` : ""}`);
    };

    const handleRemoveAlt = async (e) => {
      const { alternativeId } = e.detail;
      const prevSnapshot = itemsRef.current; // Snapshot cho rollback

      // Optimistic UI Update
      setItems((prev) => {
        const newItems = { ...prev };
        for (const key in newItems) {
           if (key === 'ideas') continue;
           newItems[key] = newItems[key].map(item => ({
              ...item,
              alternatives: item.alternatives?.filter(a => a.id !== alternativeId) || []
           }));
        }
        return newItems;
      });

      try {
        const res = await tripLocationApi.removeAlternative(alternativeId);
        if (res.isSuccess || res.success) {
          toast.success("Đã xóa địa điểm dự phòng");
          onTripUpdate?.();
        } else {
          setItems(prevSnapshot); // Rollback
          toast.error("Không thể xóa địa điểm dự phòng");
        }
      } catch {
        setItems(prevSnapshot); // Rollback
        toast.error("Lỗi xóa địa điểm dự phòng");
      }
    };

    const handleSwapAlt = async (e) => {
      const { tripLocationId, alternativeId } = e.detail;
      const prevSnapshot = itemsRef.current; // Snapshot cho rollback

      // Optimistic UI Update
      setItems((prev) => {
        const newItems = { ...prev };
        for (const key in newItems) {
          if (key === "ideas") continue;
          
          const index = newItems[key].findIndex(i => i.tripLocationId === tripLocationId);
          if (index > -1) {
            const list = [...newItems[key]];
            const primaryItem = { ...list[index] };
            const altIndex = primaryItem.alternatives?.findIndex(a => a.id === alternativeId);
            
            if (altIndex > -1) {
              const selectedAlt = primaryItem.alternatives[altIndex];
              
              // Tạo alternative mới đẩy từ primary cũ xuống
              const newAlt = {
                id: alternativeId, // Giữ nguyên ID UI
                tripLocationId: tripLocationId,
                locationId: primaryItem.locationId,
                locationName: primaryItem.content,
                locationAddress: "Đã chuyển từ địa điểm chính",
                images: JSON.stringify([primaryItem.image]),
              };
              
              // Trích xuất hình ảnh alternative (nếu parse được)
              let altImage = "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop";
              try {
                const parsed = JSON.parse(selectedAlt.images || "[]");
                altImage = (parsed[0]?.url || parsed[0]) || altImage;
              } catch (e) {}

              // Đảo ngược vị trí primaryItem mới 
              const newPrimaryItem = {
                ...primaryItem,
                locationId: selectedAlt.locationId,
                content: selectedAlt.locationName,
                image: altImage,
                alternatives: primaryItem.alternatives.map(a => a.id === alternativeId ? newAlt : a)
              };
              
              list[index] = newPrimaryItem;
              newItems[key] = list;
            }
            break;
          }
        }
        return newItems;
      });

      try {
        const res = await tripLocationApi.swapPrimary(tripLocationId, alternativeId);
        if (res.isSuccess || res.success) {
          toast.success("Đã đổi địa điểm chính thành công");
          onTripUpdate?.();
        } else {
          setItems(prevSnapshot); // Rollback
          toast.error("Không thể đổi địa điểm chính");
        }
      } catch {
        setItems(prevSnapshot); // Rollback
        toast.error("Lỗi khi đổi địa điểm chính");
      }
    };

    window.addEventListener("pickAlternative", handlePickAlt);
    window.addEventListener("removeAlternative", handleRemoveAlt);
    window.addEventListener("swapAlternative", handleSwapAlt);

    return () => {
      window.removeEventListener("pickAlternative", handlePickAlt);
      window.removeEventListener("removeAlternative", handleRemoveAlt);
      window.removeEventListener("swapAlternative", handleSwapAlt);
    };
  }, [tripData, onTripUpdate, destination]);

  const handleItemHover = React.useCallback((id) => {
    onItemHover?.(id);
  }, [onItemHover]);
  const handleItemLeave = React.useCallback(() => {
    onItemLeave?.();
  }, [onItemLeave]);

  return (
    <React.Fragment key="trip-details-drawer">
      {/* Mobile Backdrop */}
      <div
        className={`fixed inset-0 bg-gray-900/40 backdrop-blur-sm z-40 md:hidden transition-opacity duration-300 ease-out ${
          isOpen ? "opacity-100" : "opacity-0 pointer-events-none"
        }`}
        onClick={onClose}
      />

      {/* Drawer Panel */}
      <div
        className={`fixed bg-white flex flex-col z-50 overflow-hidden transition-transform duration-300 ease-out
          inset-x-0 bottom-0 top-[12%] rounded-t-[2rem] shadow-[0_-12px_40px_rgb(0,0,0,0.16)]
          md:top-16 md:bottom-0 md:w-1/2 md:rounded-none md:shadow-2xl md:border-gray-200
          ${isExpanded ? "md:left-0 md:border-r" : "md:right-0 md:left-auto md:border-l"}
          ${
            isOpen
              ? "translate-y-0 md:translate-x-0"
              : `translate-y-full md:translate-y-0 ${isExpanded ? "md:-translate-x-full" : "md:translate-x-full"} pointer-events-none`
          }
        `}
      >
        {/* Mobile Handle Bar */}
        <div
          className="w-full h-8 flex items-center justify-center md:hidden shrink-0 cursor-pointer bg-white"
          onClick={onClose}
        >
          <div className="w-12 h-1.5 bg-gray-200 hover:bg-gray-300 rounded-full transition-colors" />
        </div>

        {/* Global Click Outside Overlay for Dropdowns */}
        {activeMenuId && (
          <div
            className="fixed inset-0 z-50"
            onClick={() => setActiveMenuId(null)}
          />
        )}

        {/* 1. Top Navigation Bar */}
        <TripDrawerTopBar
          onClose={onClose}
          onShareClick={() => canEdit && setIsInviteModalOpen(true)}
          isExpanded={isExpanded}
          onToggleExpand={() => setIsExpanded(!isExpanded)}
          ownerName={tripData?.userName}
          members={members}
          canEdit={canEdit}
          canModifyMembers={canModifyMembers}
          isPublic={isPublic}
          onToggleVisibility={handleToggleVisibility}
        />

        {/* 2. Scrollable Content Area (Header + Tabs + Body) */}
        <div className="flex-1 grid grid-cols-1 overflow-hidden">
          {/* LEFT SIDE: Itinerary Content */}
          <div className="flex-1 flex flex-col overflow-y-auto bg-slate-50 relative no-scrollbar">
            {/* 2.1 Trip Info Header (Scrolls away) */}
            <TripInfoHeader
              tripTitle={tripTitle}
              onTitleChange={onTitleChange}
              onTitleSave={onTitleSave}
              startDate={startDate}
              endDate={endDate}
              onDateClick={() => canEdit && setIsEditDateOpen(true)}
              tripSize={tripSize}
              onTripSizeChange={onTripSizeChange}
              onTripSizeSave={onTripSizeSave}
              canEdit={canEdit}
              destination={destination}
            />

            {/* 2.2 Tabs (Sticky) */}
            <TripDrawerTabs
              activeTab={activeTab}
              onTabChange={setActiveTab}
            />

            {/* 2.3 Content Body */}
            <div className="min-h-[calc(100%-140px)]">
              <TripTabContent
                activeTab={activeTab}
                tripData={tripData}
                tripDayIdMap={tripDayIdMap}
                dayIds={dayIds}
                items={items}
                setItems={setItems}
                handleItemHover={handleItemHover}
                handleItemLeave={handleItemLeave}
                canEdit={canEdit}
                onReorderSuccess={onReorderSuccess}
                onRoleUpdateSuccess={onRoleUpdateSuccess}
                setIsAddModalOpen={setIsAddModalOpen}
                setSelectedLocation={setSelectedLocation}
                setIsLocationDrawerOpen={setIsLocationDrawerOpen}
                handleAddToSchedule={handleAddToSchedule}
                handleSwapLocation={handleSwapLocation}
                handleQuickAddToDay={handleQuickAddToDay}
                dayOptions={dayOptions}
                handleMoveToIdeas={handleMoveToIdeas}
                requestDelete={requestDelete}
                handleToggleMenu={handleToggleMenu}
                getDateLabel={getDateLabel}
                handleEditTime={handleEditTime}
                handleTimeChange={handleTimeChange}
                handleNoteChange={handleNoteChange}
                activeMenuId={activeMenuId}
                currentUser={currentUser}
                members={members}
                fetchMembers={fetchMembers}
              />
            </div>
          </div>
        </div>
      </div>

      <TripModals
        isAddModalOpen={isAddModalOpen}
        setIsAddModalOpen={setIsAddModalOpen}
        handleConfirmAddLocation={handleConfirmAddLocation}
        isInviteModalOpen={isInviteModalOpen}
        setIsInviteModalOpen={setIsInviteModalOpen}
        tripData={tripData}
        isLocationDrawerOpen={isLocationDrawerOpen}
        setIsLocationDrawerOpen={setIsLocationDrawerOpen}
        selectedLocation={selectedLocation}
        setSelectedLocation={setSelectedLocation}
        isEditDateOpen={isEditDateOpen}
        setIsEditDateOpen={setIsEditDateOpen}
        startDate={startDate}
        endDate={endDate}
        onDateChange={onDateChange}
        items={items}
        isEditTimeModalOpen={isEditTimeModalOpen}
        setIsEditTimeModalOpen={setIsEditTimeModalOpen}
        handleConfirmEditTime={handleConfirmEditTime}
        editingItem={editingItem}
        deleteConfirmation={deleteConfirmation}
        setDeleteConfirmation={setDeleteConfirmation}
        handleConfirmDelete={handleConfirmDelete}
        isScheduleModalOpen={isScheduleModalOpen}
        setIsScheduleModalOpen={setIsScheduleModalOpen}
        setSelectedIdeaId={setSelectedIdeaId}
        handleConfirmSchedule={handleConfirmSchedule}
        dayIds={dayIds}
        getDateLabel={getDateLabel}
        selectedIdeaId={selectedIdeaId}
        isSwapModalOpen={isSwapModalOpen}
        setIsSwapModalOpen={setIsSwapModalOpen}
        setSelectedSwapIdeaId={setSelectedSwapIdeaId}
        handleConfirmSwap={handleConfirmSwap}
        selectedSwapIdeaId={selectedSwapIdeaId}
      />
    </React.Fragment>
  );
};

// ... existing exports ...

TripDetailsDrawer.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  tripData: PropTypes.shape({
    id: PropTypes.string,
    tripDays: PropTypes.array,
    // Add other relevant tripData fields if needed
  }),
  onTripUpdate: PropTypes.func, // Optional callback after updates
};

export default React.memo(TripDetailsDrawer);
