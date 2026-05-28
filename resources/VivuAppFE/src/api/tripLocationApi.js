import axiosClient from "./axiosClient";

const tripLocationApi = {
    /**
     * Add a location to a trip day
     * @param {Object} data - { tripDayId, locationId, orderIndex, startTime?, endTime?, note?, transportMode? }
     */
    add: (data) => {
        return axiosClient.post('/triplocation', data);
    },

    /**
     * Update a trip location
     * @param {string} id - The trip location ID
     * @param {Object} data - { tripDayId, locationId, orderIndex, startTime?, endTime?, note?, transportMode? }
     */
    update: (id, data) => {
        return axiosClient.put(`/triplocation/${id}`, data);
    },

    /**
     * Remove a trip location
     * @param {string} id - The trip location ID
     */
    remove: (id) => {
        return axiosClient.delete(`/triplocation/${id}`);
    },

    /**
     * Reorder trip locations within a trip day
     * @param {Object} data - { tripDayId, OrderedTripLocationIds }
     */
    reorder: (data) => {
        return axiosClient.put('/triplocation/reorder', data);
    },

    // ===== Alternative Locations =====

    /**
     * Add an alternative location to a trip location
     * @param {string} tripLocationId - The primary trip location ID
     * @param {Object} data - { locationId, reason? }
     */
    addAlternative: (tripLocationId, data) => {
        return axiosClient.post(`/triplocation/${tripLocationId}/alternatives`, data);
    },

    /**
     * Remove an alternative location
     * @param {string} alternativeId - The alternative location ID
     */
    removeAlternative: (alternativeId) => {
        return axiosClient.delete(`/triplocation/alternatives/${alternativeId}`);
    },

    /**
     * Swap an alternative location to become the primary location
     * @param {string} tripLocationId - The primary trip location ID
     * @param {string} alternativeId - The alternative location ID
     */
    swapPrimary: (tripLocationId, alternativeId) => {
        return axiosClient.post(`/triplocation/${tripLocationId}/alternatives/${alternativeId}/swap`);
    }
};

export default tripLocationApi;
