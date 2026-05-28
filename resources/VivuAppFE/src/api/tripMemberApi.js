import axiosClient from "./axiosClient";

const tripMemberApi = {
  // Get all members of a trip
  // GET /api/tripmembers/{tripId}
  getMembers: (tripId, page = 1, pageSize = 20) => {
    return axiosClient.get(`/tripmembers/${tripId}`, {
      params: { pageNumber: page, pageSize },
    });
  },

  // Remove a member (Owner only)
  // POST /api/tripmembers/remove-member
  removeMember: (tripId, userId) => {
    return axiosClient.post("/tripmembers/remove-member", {
      tripId,
      userId,
    });
  },

  // Leave a trip
  // DELETE /api/tripmembers/{tripId}/leave
  leaveTrip: (tripId) => {
    return axiosClient.delete(`/tripmembers/${tripId}/leave`);
  },

  // Update member role (Owner only)
  // PATCH /api/trips/{tripId}/members/{memberUserId}
  // Data: { role: "Editor" | "Viewer" }
  updateMemberRole: (tripId, memberUserId, role) => {
    return axiosClient.patch(`/trips/${tripId}/members/${memberUserId}`, {
      newRole: role,
    });
  },

  // Join trip by code
  // POST /api/trips/join
  joinTrip: (inviteCode) => {
    return axiosClient.post("/trips/join", { inviteCode });
  },
};

export default tripMemberApi;
