import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7294/api";

const axiosClient = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

axiosClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("access_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Flag to prevent multiple refresh requests at once
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

axiosClient.interceptors.response.use(
  (response) => {
    return response.data;
  },
  async (error) => {
    const originalRequest = error.config;

    // Filter out login/refresh endpoints to prevent loops
    if (
      !originalRequest ||
      originalRequest.url.includes("/auth/login") ||
      originalRequest.url.includes("/auth/refresh-token")
    ) {
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return axiosClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const refreshToken = localStorage.getItem("refresh_token");
        const accessToken = localStorage.getItem("access_token");

        if (!refreshToken) {
          throw new Error("No refresh token");
        }

        const response = await axios.post(`${API_URL}/auth/refresh-token`, {
          accessToken,
          refreshToken,
        });

        // Check if response has data and success flag (backend wrapper)
        if (response.data && response.data.success) {
          const { accessToken: newAccessToken, refreshToken: newRefreshToken } =
            response.data.data;

          localStorage.setItem("access_token", newAccessToken);
          localStorage.setItem("refresh_token", newRefreshToken);

          axiosClient.defaults.headers.common.Authorization = `Bearer ${newAccessToken}`;
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

          processQueue(null, newAccessToken);
          return axiosClient(originalRequest);
        } else {
          throw new Error("Refresh failed: Invalid response format");
        }
      } catch (refreshError) {
        processQueue(refreshError, null);
        localStorage.removeItem("access_token");
        localStorage.removeItem("refresh_token");
        localStorage.removeItem("user");
        // Only redirect if not already on login page
        if (!window.location.pathname.includes("/login")) {
          // Vercel Rule: bundle-conditional
          import("../utils/toast").then(({ default: toast }) => {
            toast.error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
          });
          setTimeout(() => {
            window.location.href = "/login";
          }, 1500);
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);

export default axiosClient;
