import { describe, it, expect, vi, beforeEach } from "vitest";
import authApi from "../authApi";
import axiosClient from "../axiosClient";

vi.mock("../axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe("authApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("register", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com", password: "password" };
      const result = await authApi.register(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/auth/register", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("login", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com", password: "password" };
      const result = await authApi.login(payload);

      expect(axiosClient.post).toHaveBeenCalledWith("/auth/login", payload);
      expect(result).toEqual(mockResponse);
    });
  });

  describe("getMe", () => {
    it("should call axiosClient.get with correct URL", async () => {
      const mockResponse = { data: { id: "u1" } };
      axiosClient.get.mockResolvedValueOnce(mockResponse);

      const result = await authApi.getMe();

      expect(axiosClient.get).toHaveBeenCalledWith("/auth/me");
      expect(result).toEqual(mockResponse);
    });
  });

  describe("sendVerificationEmail", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com" };
      const result = await authApi.sendVerificationEmail(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/send-verification-email",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("forgotPassword", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com" };
      const result = await authApi.forgotPassword(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/forgot-password",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("verifyResetOtp", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { email: "test@test.com", otp: "123456" };
      const result = await authApi.verifyResetOtp(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/verify-reset-otp",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("resetPassword", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { newPassword: "newpassword" };
      const result = await authApi.resetPassword(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/reset-password",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("googleLogin", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { credential: "token" };
      const result = await authApi.googleLogin(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/google-login",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe("refreshToken", () => {
    it("should call axiosClient.post with correct URL and payload", async () => {
      const mockResponse = { data: { success: true } };
      axiosClient.post.mockResolvedValueOnce(mockResponse);

      const payload = { token: "token" };
      const result = await authApi.refreshToken(payload);

      expect(axiosClient.post).toHaveBeenCalledWith(
        "/auth/refresh-token",
        payload,
      );
      expect(result).toEqual(mockResponse);
    });
  });
});
