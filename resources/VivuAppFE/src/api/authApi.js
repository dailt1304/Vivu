import axiosClient from "./axiosClient";

const authApi = {
  register: (data) => {
    return axiosClient.post("/auth/register", data);
  },
  login: (data) => {
    return axiosClient.post("/auth/login", data);
  },
  getMe: () => {
    return axiosClient.get("/auth/me");
  },
  sendVerificationEmail: (data) => {
    return axiosClient.post("/auth/send-verification-email", data);
  },

  forgotPassword: (data) => {
    return axiosClient.post("/auth/forgot-password", data);
  },
  verifyResetOtp: (data) => {
    return axiosClient.post("/auth/verify-reset-otp", data);
  },
  resetPassword: (data) => {
    return axiosClient.post("/auth/reset-password", data);
  },
  googleLogin: (data) => {
    return axiosClient.post("/auth/google-login", data);
  },
  refreshToken: (data) => {
    return axiosClient.post("/auth/refresh-token", data);
  },
};

export default authApi;
