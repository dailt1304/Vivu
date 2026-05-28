import axiosClient from "./axiosClient";

const locationApi = {
  /**
   * Lấy địa điểm phổ biến nhất (AllowAnonymous)
   * @param {Object} params - { pageNumber?, pageSize? }
   */
  getPopular: (params) => {
    return axiosClient.get("/locations/popular", { params });
  },

  /**
   * Lấy địa điểm gần vị trí cho trước (Requires Auth)
   * @param {Object} params - { latitude, longitude, radiusInMeters?, categoryId?, limit?, minRating? }
   */
  getNearby: (params) => {
    return axiosClient.get("/locations/nearby", { params });
  },

  /**
   * Get all locations with pagination
   * @param {Object} params - { pageNumber, pageSize }
   */
  getAll: (params) => {
    return axiosClient.get("/locations", { params });
  },

  /**
   * Get locations by filters
   * @param {Object} params - CityId, CategoryId, MinRating, IsVerifiedOnly, SortBy, IsDescending, etc.
   */
  getByFilter: (params) => {
    return axiosClient.get("/locations/get-by-filter", { params });
  },

  /**
   * Get location details by ID
   * @param {string} id - Location ID
   */
  getById: (id) => {
    return axiosClient.get(`/locations/${id}`);
  },

  /**
   * Submit new location (multipart/form-data)
   * @param {FormData} formData
   */
  submit: (formData) => {
    return axiosClient.post("/locations", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Tạo địa điểm mới trực tiếp (Moderator/Admin only)
   * @param {FormData} formData
   */
  create: (formData) => {
    return axiosClient.post("/locations/create", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Report location
   * @param {FormData} formData
   */
  report: (formData) => {
    return axiosClient.post("/locations/report", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Get user's submitted locations
   * @param {Object} params - { pageNumber, pageSize, status }
   */
  getMySubmissions: (params) => {
    return axiosClient.get("/locations/my-submissions", { params });
  },

  /**
   * Lấy danh sách locations đang chờ duyệt
   */
  getPending: (params) => {
    return axiosClient.get("/locations/pending", { params });
  },

  /**
   * Tìm kiếm locations theo từ khóa
   */
  searchTerm: (params) => {
    return axiosClient.post("/locations/search-term", null, { params });
  },

  /**
   * Cập nhật location
   */
  update: (id, data) => {
    return axiosClient.put(`/locations/${id}`, data);
  },

  /**
   * Đề xuất chỉnh sửa
   */
  suggestUpdate: (id, formData) => {
    return axiosClient.put(`/locations/${id}/suggest-update`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
  },

  /**
   * Xóa location
   */
  delete: (id) => {
    return axiosClient.delete(`/locations/${id}`);
  },

  /**
   * Từ chối đề xuất location
   */
  rejectSuggestion: (id, data) => {
    return axiosClient.post(`/locations/${id}/reject-suggestion`, data);
  },

  /**
   * Duyệt location
   */
  approve: (id, data) => {
    return axiosClient.put(`/locations/${id}/approve`, data);
  },
  /**
   * Lấy danh sách điểm đến nổi bật (Trending)
   * @param {Object} params - { LocationsPerCity, PageNumber, PageSize, SortColumn, SortDescending }
   */
  getTrending: (params) => {
    return axiosClient.get("/Destinations/trending", { params });
  },
};

export default locationApi;
