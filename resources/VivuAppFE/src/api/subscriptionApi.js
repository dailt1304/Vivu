import axiosClient from "./axiosClient";

/**
 * API client for Subscription related operations.
 */
const subscriptionApi = {
  /**
   * Get all subscription plans (Admin).
   */
  getAll: (params) => {
    return axiosClient.get("/subscription-packages", { params });
  },

  /**
   * Get subscription plan by ID.
   * @param {string} id - Plan ID
   */
  getById: (id) => {
    return axiosClient.get(`/subscription-packages/${id}`);
  },

  /**
   * Create a new subscription plan.
   * @param {Object} data - Plan data
   */
  create: (data) => {
    return axiosClient.post("/subscription-packages", data);
  },

  /**
   * Update an existing subscription plan.
   * @param {string} id - Plan ID
   * @param {Object} data - Updated plan data
   */
  update: (id, data) => {
    return axiosClient.put(`/subscription-packages/${id}`, data);
  },

  /**
   * Delete a subscription plan.
   * @param {string} id - Plan ID
   */
  delete: (id) => {
    return axiosClient.delete(`/subscription-packages/${id}`);
  },

  /**
   * Get active subscription plans for users.
   */
  getActivePackages: () => {
    return axiosClient.get("/subscription-packages/active");
  },

  /**
   * Subscribe to a package.
   * @param {string} packageId - The ID of the subscription package.
   */
  subscribe: (packageId) => {
    return axiosClient.post(`/subscription-packages/${packageId}/subscribe`);
  },

  /**
   * Get current user's subscription and usage stats.
   * Reuse the existing /users/usage endpoint.
   */
  getMySubscription: () => {
    return axiosClient.get("/users/usage");
  },
};

export default subscriptionApi;
