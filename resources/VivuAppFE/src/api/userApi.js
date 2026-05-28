import axiosClient from "./axiosClient";

const userApi = {
  getById: (id) => {
    return axiosClient.get(`/users/${id}`);
  },

  getAll: (params) => {
    return axiosClient.get("/users", { params });
  },

  search: (params) => {
    return axiosClient.get("/users/search", { params });
  },

  create: (data) => {
    return axiosClient.post("/users", data);
  },

  getMyFavoriteTrips: (params) => {
    return axiosClient.get("/users/me/favorites", { params });
  },

  getUsageStats: () => {
    return axiosClient.get("/users/usage");
  },

  updateMyProfile: (data) => {
    return axiosClient.put("/users/me/profile", data);
  },

  ban: (id, reason) => {
    return axiosClient.post(`/users/${id}/ban`, { reason });
  },

  unban: (id) => {
    return axiosClient.post(`/users/${id}/unban`);
  },

  uploadAvatar: (file) => {
    const formData = new FormData();
    formData.append("Avatar", file);
    return axiosClient.post("/users/me/avatar", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },
};

export default userApi;
