import axiosClient from "./axiosClient";

const locationCategoryApi = {
  getAll: (params) => {
    return axiosClient.get("/location-categories", { params });
  },

  create: (data) => {
    return axiosClient.post("/location-categories", data);
  },

  update: (id, data) => {
    return axiosClient.put(`/location-categories/${id}`, data);
  },

  delete: (id) => {
    return axiosClient.delete(`/location-categories/${id}`);
  },

  changeStatus: (id, data) => {
    return axiosClient.put(`/location-categories/${id}/status`, data);
  },
};

export default locationCategoryApi;
