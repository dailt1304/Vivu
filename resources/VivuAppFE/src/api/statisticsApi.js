import axiosClient from "./axiosClient";

const statisticsApi = {
  getLocationStats: () => axiosClient.get("/statistics/locations"),
  getDashboardStats: () => axiosClient.get("/statistics/dashboard"),
};

export default statisticsApi;
