import React from "react";
import AddLocationModal from "../modals/AddLocationModal";
import InviteMemberModal from "../members/InviteMemberModal";
import DestinationDrawer from "../../../common/drawers/DestinationDrawer";
import EditDateModal from "../modals/EditDateModal";
import EditTimeModal from "../modals/EditTimeModal";
import ConfirmationModal from "../../../common/modals/ConfirmationModal";
import AddToScheduleModal from "../modals/AddToScheduleModal";
import SwapLocationModal from "../modals/SwapLocationModal";

function TripModals({
  // Add Location Modal
  isAddModalOpen,
  setIsAddModalOpen,
  handleConfirmAddLocation,

  // Invite Member Modal
  isInviteModalOpen,
  setIsInviteModalOpen,
  tripData,

  // Destination Drawer
  isLocationDrawerOpen,
  setIsLocationDrawerOpen,
  selectedLocation,
  setSelectedLocation,

  // Edit Date Modal
  isEditDateOpen,
  setIsEditDateOpen,
  startDate,
  endDate,
  onDateChange,
  items,

  // Edit Time Modal
  isEditTimeModalOpen,
  setIsEditTimeModalOpen,
  handleConfirmEditTime,
  editingItem,

  // Confirmation Modal
  deleteConfirmation,
  setDeleteConfirmation,
  handleConfirmDelete,

  // Add to Schedule Modal
  isScheduleModalOpen,
  setIsScheduleModalOpen,
  setSelectedIdeaId,
  handleConfirmSchedule,
  dayIds,
  getDateLabel,
  selectedIdeaId,

  // Swap Location Modal
  isSwapModalOpen,
  setIsSwapModalOpen,
  setSelectedSwapIdeaId,
  handleConfirmSwap,
  selectedSwapIdeaId,
}) {
  return (
    <>
      {isAddModalOpen && (
        <AddLocationModal
          isOpen={isAddModalOpen}
          onClose={() => setIsAddModalOpen(false)}
          onAddLocation={handleConfirmAddLocation}
          existingLocationIds={items?.ideas?.map((i) => i.locationId).filter(Boolean) || []}
          cityId={tripData?.cityId}
        />
      )}

      {isInviteModalOpen && (
        <InviteMemberModal
          isOpen={isInviteModalOpen}
          onClose={() => setIsInviteModalOpen(false)}
          inviteCode={tripData?.inviteCode}
          tripTitle={tripData?.title}
        />
      )}

      {isLocationDrawerOpen && (
        <DestinationDrawer
          isOpen={isLocationDrawerOpen}
          onClose={() => {
            setIsLocationDrawerOpen(false);
            setSelectedLocation(null);
          }}
          destination={selectedLocation}
          data={selectedLocation}
        />
      )}

      {isEditDateOpen && (
        <EditDateModal
          isOpen={isEditDateOpen}
          onClose={() => setIsEditDateOpen(false)}
          startDate={startDate}
          endDate={endDate}
          onSave={onDateChange}
          occupiedDayIndices={
            (tripData?.tripDays || [])
              .filter((day) => day.dayIndex > 0 && day.locations?.length > 0)
              .map((day) => day.dayIndex)
          }
        />
      )}

      {isEditTimeModalOpen && (
        <EditTimeModal
          isOpen={isEditTimeModalOpen}
          onClose={() => setIsEditTimeModalOpen(false)}
          onConfirm={handleConfirmEditTime}
          initialStartTime={editingItem?.startTime}
          initialEndTime={editingItem?.endTime}
          itemTitle={editingItem?.content}
        />
      )}

      {deleteConfirmation.isOpen && (
        <ConfirmationModal
          isOpen={deleteConfirmation.isOpen}
          onClose={() =>
            setDeleteConfirmation({ ...deleteConfirmation, isOpen: false })
          }
          onConfirm={handleConfirmDelete}
          title="Xóa địa điểm"
          message="Bạn có chắc chắn muốn xóa địa điểm này khỏi lịch trình không?"
          confirmText="Xóa"
          cancelText="Hủy"
        />
      )}

      {isScheduleModalOpen && (
        <AddToScheduleModal
          isOpen={isScheduleModalOpen}
          onClose={() => {
            setIsScheduleModalOpen(false);
            setSelectedIdeaId(null);
          }}
          onConfirm={handleConfirmSchedule}
          days={dayIds.map((id, index) => ({
            id,
            label: `Ngày ${index + 1} - ${getDateLabel(index)}`,
            date: getDateLabel(index),
          }))}
          itemTitle={
            selectedIdeaId
              ? items.ideas.find((i) => i.id === selectedIdeaId)?.content
              : ""
          }
        />
      )}
      {isSwapModalOpen && (
        <SwapLocationModal
          isOpen={isSwapModalOpen}
          onClose={() => {
            setIsSwapModalOpen(false);
            setSelectedSwapIdeaId(null);
          }}
          onConfirm={handleConfirmSwap}
          items={items}
          dayIds={dayIds}
          getDateLabel={getDateLabel}
          ideaTitle={
            selectedSwapIdeaId
              ? items?.ideas?.find((i) => i.id === selectedSwapIdeaId)?.content
              : ""
          }
        />
      )}
    </>
  );
}

export default React.memo(TripModals);
