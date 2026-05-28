import axiosClient from "./axiosClient";

const collectionApi = {
  getAll: (params) => axiosClient.get("/collections", { params }),
  
  getSummary: () => axiosClient.get("/collections/summary"),
  
  getById: (id) => axiosClient.get(`/collections/${id}`),
  
  create: (formData) => 
    axiosClient.post("/collections", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    }),
    
  update: (id, formData) => 
    axiosClient.put(`/collections/${id}`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    }),
    
  delete: (id) => axiosClient.delete(`/collections/${id}`),
  
  addLocation: (id, data) => 
    axiosClient.post(`/collections/${id}/locations`, data),
    
  removeLocation: (id, locationId) =>
    axiosClient.delete(`/collections/${id}/locations/${locationId}`),
};

export default collectionApi;
