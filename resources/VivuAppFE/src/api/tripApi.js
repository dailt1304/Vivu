import axiosClient from "./axiosClient";

const tripApi = {
  // AllowAnonymous endpoint to get public trips
  getPublic: (params) => {
    return axiosClient.get("/trips/public", { params });
  },

  // Full-text search public trips
  searchPublic: (params) => {
    return axiosClient.get("/trips/public/search", { params });
  },

  // Get public trip detail by ID (AllowAnonymous)
  getPublicById: (tripId) => {
    return axiosClient.get(`/trips/public/${tripId}`);
  },

  getAllByUser: (userId, params) => {
    return axiosClient.get(`/trips/user/${userId}`, { params });
  },

  getUserTrips: (userId, params) => {
    return axiosClient.get(`/trips/user/${userId}`, { params });
  },

  getById: (id) => {
    return axiosClient.get(`/trips/${id}`);
  },

  create: (data) => {
    return axiosClient.post("/trips", data);
  },

  update: (id, data) => {
    return axiosClient.put(`/trips/${id}`, data);
  },

  delete: (id) => {
    return axiosClient.delete(`/trips/${id}`);
  },

  getCompleted: (userId, pageNumber = 1, pageSize = 10) => {
    return axiosClient.get(`/trips/completed/${userId}`, {
      params: { pageNumber, pageSize },
    });
  },

  rate: (tripId, data) => {
    return axiosClient.post(`/trips/${tripId}/rate`, data);
  },

  // Favorite/Unfavorite
  favorite: (tripId) => axiosClient.post(`/trips/${tripId}/favorite`),
  unfavorite: (tripId) => axiosClient.delete(`/trips/${tripId}/favorite`),

  // Visibility
  updateVisibility: (tripId, data) =>
    axiosClient.patch(`/trips/${tripId}/visibility`, data),

  // Chat History & Messages
  getChatHistory: (tripId, params) =>
    axiosClient.get(`/trips/${tripId}/messages`, { params }),
  deleteChatMessage: (tripId, messageId) =>
    axiosClient.delete(`/trips/${tripId}/messages/${messageId}`),

  // Trip Gallery Images & Image Upload
  getTripImages: (tripId) => axiosClient.get(`/trips/${tripId}/images`),
  sendChatImage: (tripId, formData) =>
    axiosClient.post(`/trips/${tripId}/chat/images`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    }),

  // Copy Trip
  copy: (tripId) => axiosClient.post(`/trips/${tripId}/copy`, {}),

  // Batch Update Statuses
  updateStatuses: () => axiosClient.post("/trips/update-statuses"),
};

export default tripApi;
