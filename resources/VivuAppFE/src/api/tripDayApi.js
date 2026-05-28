import axiosClient from "./axiosClient";

const tripDayApi = {
    /**
     * Add a new day to a trip
     * @param {Object} data - { tripId, title?, dayDate? }
     */
    add: (data) => {
        return axiosClient.post('/tripday', data);
    },

    /**
     * Update a trip day
     * @param {string} tripDayId - The trip day ID
     * @param {Object} data - { title?, dayDate? }
     */
    update: (tripDayId, data) => {
        return axiosClient.put(`/tripday/${tripDayId}`, data);
    },

    /**
     * Remove a trip day
     * @param {string} tripDayId - The trip day ID
     */
    remove: (tripDayId) => {
        return axiosClient.delete(`/tripday/${tripDayId}`);
    },

    /**
     * Reorder trip days within a trip
     * @param {string} tripId - The trip ID
     * @param {Array} orderedTripDayIds - Array of trip day IDs in new order
     */
    reorder: (tripId, orderedTripDayIds) => {
        return axiosClient.put(`/tripday/reorder/${tripId}`, { orderedTripDayIds });
    }
};

export default tripDayApi;
