import axiosClient from "./axiosClient";

const notificationApi = {
  /**
   * Get all notifications with pagination and filtering
   * @param {Object} params - { unreadOnly, type, pageNumber, pageSize }
   */
  getAll: (params) => axiosClient.get("/notifications", { params }),

  /**
   * Get unread notification count
   */
  getUnreadCount: () => axiosClient.get("/notifications/unread-count"),

  /**
   * Mark a specific notification as read
   * @param {string} id - Notification ID
   */
  markAsRead: (id) => axiosClient.patch(`/notifications/${id}/read`),

  /**
   * Mark all notifications as read
   */
  markAllAsRead: () => axiosClient.patch("/notifications/read-all"),

  /**
   * Delete a notification
   * @param {string} id - Notification ID
   */
  delete: (id) => axiosClient.delete(`/notifications/${id}`),
};

export default notificationApi;
