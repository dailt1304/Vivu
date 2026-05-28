import axiosClient from "./axiosClient";

/**
 * API client for City related operations.
 */
const cityApi = {
  /**
   * Get all cities with pagination and optional country filter.
   * @param {Object} params - { pageNumber, pageSize, countryId?, sortColumn?, sortDescending? }
   */
  getAll: (params) => {
    return axiosClient.get("/cities", { params });
  },

  /**
   * Get city details by ID.
   * @param {string} id - City ID (GUID)
   */
  getById: (id) => {
    return axiosClient.get(`/cities/${id}`);
  },

  /**
   * Search cities by text.
   * @param {Object} params - { searchText, pageNumber, pageSize }
   */
  search: (params) => {
    return axiosClient.get("/cities/search", { params });
  },

  /**
   * Create a new city.
   * @param {Object} data - { name, countryId, latitude?, longitude? }
   */
  create: (data) => {
    return axiosClient.post("/cities", data);
  },

  /**
   * Update an existing city.
   * @param {string} id - City ID
   * @param {Object} data - { name?, countryId?, latitude?, longitude? }
   */
  update: (id, data) => {
    return axiosClient.put(`/cities/${id}`, data);
  },

  /**
   * Delete a city.
   * @param {string} id - City ID
   */
  delete: (id) => {
    return axiosClient.delete(`/cities/${id}`);
  },
};

export default cityApi;
