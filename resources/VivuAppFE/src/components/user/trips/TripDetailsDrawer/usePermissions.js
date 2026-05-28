import { useMemo } from "react";

/**
 * usePermissions - Trích xuất logic phân quyền trip
 * Vercel Best Practice: Co-located custom hook
 */
export function usePermissions(tripData, currentUser, userId, members) {
  return useMemo(() => {
    const currentUserId = userId || currentUser?.id || currentUser?.userId;
    const isOwner =
      tripData?.userId && currentUserId
        ? String(tripData.userId) === String(currentUserId)
        : false;
    const currentUserMember = members.find(
      (m) => String(m.userId) === String(currentUserId),
    );
    const userRole = (currentUserMember?.role || tripData?.role || "")
      .toString()
      .toLowerCase();
    const canEdit = isOwner || userRole === "editor";
    const canModifyMembers = isOwner;
    return { isOwner, canEdit, canModifyMembers, currentUserId };
  }, [tripData, currentUser, userId, members]);
}
