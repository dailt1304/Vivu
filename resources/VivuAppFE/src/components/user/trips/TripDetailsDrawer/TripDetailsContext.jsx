import { createContext, useContext, useState, useCallback, useMemo } from "react";

/**
 * TripDetailsContext - Shared state for TripDetailsDrawer compound components
 *
 * This context provides all shared state and handlers that sub-components need,
 * reducing prop drilling and enabling component splitting.
 */
const TripDetailsContext = createContext(null);

export function TripDetailsProvider({
  children,
  tripData,
  tripTitle,
  startDate,
  endDate,
  tripSize,
  currentUser,
  onTitleChange,
  onTitleSave,
  onDateChange,
  onTripSizeChange,
  onTripSizeSave,
  onItemHover,
  onItemLeave,
  onClose,
}) {
  // Editing states
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [isEditingTripSize, setIsEditingTripSize] = useState(false);
  const [localTripSize, setLocalTripSize] = useState("");

  // Modal states
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isEditDateOpen, setIsEditDateOpen] = useState(false);
  const [isEditTimeModalOpen, setIsEditTimeModalOpen] = useState(false);
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
  const [isAddLocationModalOpen, setIsAddLocationModalOpen] = useState(false);
  const [isLocationDrawerOpen, setIsLocationDrawerOpen] = useState(false);

  // Selected states
  const [selectedLocation, setSelectedLocation] = useState(null);
  const [editingItem, setEditingItem] = useState(null);
  const [selectedIdeaId, setSelectedIdeaId] = useState(null);

  // Tab state
  const [activeTab, setActiveTab] = useState("itinerary");

  // Members state (fetched at drawer level)
  const [members, setMembers] = useState([]);

  // Map flyTo state
  const [flyToLocation, setFlyToLocation] = useState(null);
  const [hoveredItemId, setHoveredItemId] = useState(null);

  // Delete confirmation
  const [deleteConfirmation, setDeleteConfirmation] = useState({
    isOpen: false,
    itemId: null,
    containerId: null,
  });

  // Drawer size state
  const [isExpanded, setIsExpanded] = useState(false);

  // Handler callbacks
  const handleToggleExpand = useCallback(() => {
    setIsExpanded((prev) => !prev);
  }, []);

  const handleOpenInviteModal = useCallback(() => {
    setIsInviteModalOpen(true);
  }, []);

  const handleCloseInviteModal = useCallback(() => {
    setIsInviteModalOpen(false);
  }, []);

  const handleOpenEditDate = useCallback(() => {
    setIsEditDateOpen(true);
  }, []);

  const handleCloseEditDate = useCallback(() => {
    setIsEditDateOpen(false);
  }, []);

  const value = useMemo(() => ({
    // Props passed through
    tripData,
    tripTitle,
    startDate,
    endDate,
    tripSize,
    currentUser,

    // Callbacks passed through
    onTitleChange,
    onTitleSave,
    onDateChange,
    onTripSizeChange,
    onTripSizeSave,
    onItemHover,
    onItemLeave,
    onClose,

    // Editing states
    isEditingTitle,
    setIsEditingTitle,
    isEditingTripSize,
    setIsEditingTripSize,
    localTripSize,
    setLocalTripSize,

    // Modal states
    isInviteModalOpen,
    setIsInviteModalOpen,
    isEditDateOpen,
    setIsEditDateOpen,
    isEditTimeModalOpen,
    setIsEditTimeModalOpen,
    isScheduleModalOpen,
    setIsScheduleModalOpen,
    isAddLocationModalOpen,
    setIsAddLocationModalOpen,
    isLocationDrawerOpen,
    setIsLocationDrawerOpen,

    // Selected states
    selectedLocation,
    setSelectedLocation,
    editingItem,
    setEditingItem,
    selectedIdeaId,
    setSelectedIdeaId,

    // Tab state
    activeTab,
    setActiveTab,

    // Members state
    members,
    setMembers,

    // Map states
    flyToLocation,
    setFlyToLocation,
    hoveredItemId,
    setHoveredItemId,

    // Delete confirmation
    deleteConfirmation,
    setDeleteConfirmation,

    // Drawer size
    isExpanded,
    setIsExpanded,

    // Handlers
    handleToggleExpand,
    handleOpenInviteModal,
    handleCloseInviteModal,
    handleOpenEditDate,
    handleCloseEditDate,
  }), [
    tripData, tripTitle, startDate, endDate, tripSize, currentUser,
    onTitleChange, onTitleSave, onDateChange, onTripSizeChange, onTripSizeSave, onItemHover, onItemLeave, onClose,
    isEditingTitle, isEditingTripSize, localTripSize,
    isInviteModalOpen, isEditDateOpen, isEditTimeModalOpen, isScheduleModalOpen, isAddLocationModalOpen, isLocationDrawerOpen,
    selectedLocation, editingItem, selectedIdeaId,
    activeTab, members, flyToLocation, hoveredItemId, deleteConfirmation, isExpanded,
    handleToggleExpand, handleOpenInviteModal, handleCloseInviteModal, handleOpenEditDate, handleCloseEditDate
  ]);

  return (
    <TripDetailsContext.Provider value={value}>
      {children}
    </TripDetailsContext.Provider>
  );
}

export function useTripDetails() {
  const context = useContext(TripDetailsContext);
  if (!context) {
    throw new Error("useTripDetails must be used within TripDetailsProvider");
  }
  return context;
}

export default TripDetailsContext;
