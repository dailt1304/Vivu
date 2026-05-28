import axiosClient from "./axiosClient";

const locationReportApi = {
  getAll: (params) => axiosClient.get("/location-reports", { params }),
  review: (data) => axiosClient.put("/location-reports/review", data),
  update: (id, formData) => axiosClient.put(`/location-reports/${id}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  }),
  getMyReports: (params) => axiosClient.get("/location-reports/my-reports", { params }),
};

export default locationReportApi;
