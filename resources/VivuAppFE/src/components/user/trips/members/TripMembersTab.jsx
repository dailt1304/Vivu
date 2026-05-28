import React, { useState } from "react";
import tripMemberApi from "../../../../api/tripMemberApi";
import MemberItem from "./MemberItem";
import toast from "../../../../utils/toast";
import ConfirmationModal from "../../../common/modals/ConfirmationModal";
import { useNavigate } from "react-router-dom";

const TripMembersTab = ({
  tripId,
  tripData,
  currentUser,
  members = [],
  onMemberUpdate,
}) => {
  const navigate = useNavigate();
  // Internal fetch removed. Using props 'members'

  // Determine permissions
  const isOwner = tripData?.userId === (currentUser?.id || currentUser?.userId);

  // Delete Confirmation State
  const [deleteConfirmation, setDeleteConfirmation] = useState({
    isOpen: false,
    memberId: null,
    memberName: "",
  });

  // Leave Confirmation State
  const [leaveConfirmation, setLeaveConfirmation] = useState(false);

  const handleUpdateRole = async (memberUserId, newRole) => {
    try {
      await tripMemberApi.updateMemberRole(tripId, memberUserId, newRole);
      toast.success("Đã cập nhật quyền thành viên");
      if (onMemberUpdate) onMemberUpdate();
    } catch {
      toast.error("Lỗi khi cập nhật quyền");
    }
  };

  const handleRemoveMember = (member) => {
    setDeleteConfirmation({
      isOpen: true,
      memberId: member.userId,
      memberName: member.fullName,
    });
  };

  const confirmRemoveMember = async () => {
    const { memberId } = deleteConfirmation;
    if (!memberId) return;

    try {
      await tripMemberApi.removeMember(tripId, memberId);
      toast.success("Đã thành công mời thành viên ra khỏi nhóm");
      if (onMemberUpdate) onMemberUpdate();
    } catch {
      toast.error("Lỗi khi xóa thành viên");
    } finally {
      setDeleteConfirmation({ isOpen: false, memberId: null, memberName: "" });
    }
  };

  const handleLeaveTrip = async () => {
    setLeaveConfirmation(true);
  };

  const confirmLeaveTrip = async () => {
    try {
      await tripMemberApi.leaveTrip(tripId);
      navigate("/my-trips", { state: { tripLeftSuccess: true } });
    } catch {
      toast.error("Lỗi khi rời chuyến đi");
    } finally {
      setLeaveConfirmation(false);
    }
  };

  return (
    <div className="p-6 h-full flex flex-col">
      {/* List */}
      <div className="flex-1 overflow-y-auto -mx-2 px-2 space-y-1">
        {members.length > 0 ? (
          members.map((member) => (
            <MemberItem
              key={member.userId}
              member={member}
              currentUser={currentUser}
              isOwner={isOwner}
              onUpdateRole={handleUpdateRole}
              onRemove={() => handleRemoveMember(member)}
              onLeave={handleLeaveTrip}
            />
          ))
        ) : (
          <div className="text-center py-8 text-slate-400">
            Không tìm thấy thành viên nào
          </div>
        )}
      </div>

      <ConfirmationModal
        isOpen={deleteConfirmation.isOpen}
        onClose={() =>
          setDeleteConfirmation({
            isOpen: false,
            memberId: null,
            memberName: "",
          })
        }
        onConfirm={confirmRemoveMember}
        title="Xóa thành viên"
        message={`Bạn có chắc chắn muốn mời thành viên "${deleteConfirmation.memberName}" ra khỏi chuyến đi này không?`}
        confirmLabel="Xóa"
        cancelLabel="Hủy"
        isDanger={true}
      />

      {/* Leave Trip Confirmation Modal */}
      <ConfirmationModal
        isOpen={leaveConfirmation}
        onClose={() => setLeaveConfirmation(false)}
        onConfirm={confirmLeaveTrip}
        title="Rời chuyến đi"
        message="Bạn có chắc chắn muốn rời khỏi chuyến đi này không? Bạn sẽ không thể xem hoặc chỉnh sửa chuyến đi này nữa trừ khi được mời lại."
        confirmLabel="Rời đi"
        cancelLabel="Hủy"
        isDanger={true}
      />
    </div>
  );
};

export default TripMembersTab;
