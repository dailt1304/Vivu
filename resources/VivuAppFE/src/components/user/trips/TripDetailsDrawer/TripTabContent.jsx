import React from "react";
import { MessageCircle } from "lucide-react";
import TripItinerary from "../TripItinerary";
import TripCalendar from "../calendar/TripCalendar";
import TripMembersTab from "../members/TripMembersTab";
import TripMedia from "../media/TripMedia";

function TripTabContent({
  activeTab,
  // Itinerary props
  tripData,
  tripDayIdMap,
  dayIds,
  items,
  setItems,
  handleItemHover,
  handleItemLeave,
  canEdit,
  onRoleUpdateSuccess, // Added
  onReorderSuccess,
  setIsAddModalOpen,
  setSelectedLocation,
  setIsLocationDrawerOpen,
  handleAddToSchedule,
  handleSwapLocation,
  handleQuickAddToDay,
  dayOptions,
  handleMoveToIdeas,
  requestDelete,
  handleToggleMenu,
  getDateLabel,
  handleEditTime,
  handleTimeChange,
  handleNoteChange,
  activeMenuId,
  // Members props
  currentUser,
  members,
  fetchMembers,
  // Calendar props
}) {
  const handleOpenAddModal = React.useCallback(() => {
    if (canEdit) setIsAddModalOpen(true);
  }, [canEdit, setIsAddModalOpen]);

  const handleViewDetails = React.useCallback((item) => {
    setSelectedLocation(item);
    setIsLocationDrawerOpen(true);
  }, [setSelectedLocation, setIsLocationDrawerOpen]);

  const handleDeleteIdea = React.useCallback((id) => {
    requestDelete(id, "ideas");
  }, [requestDelete]);

  return (
    <>
      <div style={{ display: activeTab === "itinerary" ? "block" : "none" }}>
        <TripItinerary.Provider
          tripData={tripData}
          tripDayIdMap={tripDayIdMap}
          dayIds={dayIds}
          items={items}
          setItems={setItems}
          onItemHover={handleItemHover}
          onItemLeave={handleItemLeave}
          canEdit={canEdit}
          onReorderSuccess={onReorderSuccess}
        >
          <TripItinerary.Frame>
            <div className="p-6 space-y-8">
              <TripItinerary.IdeasPanel
                onAddLocation={handleOpenAddModal}
                onViewDetails={handleViewDetails}
                onAddToSchedule={handleAddToSchedule}
                onSwapLocation={handleSwapLocation}
                onQuickAddToDay={handleQuickAddToDay}
                dayOptions={dayOptions}
                onDelete={handleDeleteIdea}
                onToggleMenu={handleToggleMenu}
                activeMenuId={activeMenuId}
              />
              <TripItinerary.DayColumns
                getDateLabel={getDateLabel}
                onEditTime={handleEditTime}
                onDelete={requestDelete}
                onViewDetails={handleViewDetails}
                onTimeChange={handleTimeChange}
                onNoteChange={handleNoteChange}
                activeMenuId={activeMenuId}
                onToggleMenu={handleToggleMenu}
                onMoveToIdeas={handleMoveToIdeas}
              />
            </div>
            <TripItinerary.DragOverlay />
          </TripItinerary.Frame>
        </TripItinerary.Provider>
      </div>

      <div style={{ display: activeTab === "calendar" ? "block" : "none" }}>
        <TripCalendar
          items={items}
          dayIds={dayIds}
          getDateLabel={getDateLabel}
        />
      </div>

      <div style={{ display: activeTab === "members" ? "block" : "none" }}>
        <TripMembersTab
          tripId={tripData?.id || tripData?.Id}
          tripData={tripData}
          currentUser={currentUser}
          members={members}
          onMemberUpdate={() => {
            fetchMembers();
            if (onRoleUpdateSuccess) onRoleUpdateSuccess();
          }}
        />
      </div>

      <div style={{ display: activeTab === "media" ? "block" : "none" }}>
        <TripMedia tripId={tripData?.id} />
      </div>

      <div style={{ display: activeTab === "chat" ? "block" : "none" }}>
        <div className="p-8 text-center text-slate-400">
          <MessageCircle size={48} className="mx-auto mb-4 opacity-50" />
          <p>Tính năng Chat đang phát triển...</p>
        </div>
      </div>
    </>
  );
}

export default React.memo(TripTabContent);
