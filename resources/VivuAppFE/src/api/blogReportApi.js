import axiosClient from "./axiosClient";

const blogReportApi = {
  getAll: (params) => axiosClient.get("/blog-reports", { params }),
  review: (data) => axiosClient.put("/blog-reports/review", data),
};

export default blogReportApi;
