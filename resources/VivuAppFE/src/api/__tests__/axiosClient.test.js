import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import axiosClient from "../axiosClient";
import { http, HttpResponse } from "msw";
import { server } from "../../test/mocks/server";

// Mock window.location for redirect tests
const originalLocation = window.location;

describe("axiosClient Interceptors", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();

    delete window.location;
    window.location = {
      ...originalLocation,
      pathname: "/",
      href: "http://localhost/",
    };
  });

  afterEach(() => {
    window.location = originalLocation;
  });

  it("should add Authorization header if access_token exists", async () => {
    localStorage.setItem("access_token", "test-token");

    // Create a temporary endpoint for this test
    server.use(
      http.get("https://localhost:7294/api/test-auth", ({ request }) => {
        return HttpResponse.json({
          token: request.headers.get("Authorization"),
        });
      }),
    );

    const response = await axiosClient.get("/test-auth");
    // Interceptor unwraps response.data, so response here IS the data
    expect(response.token).toBe("Bearer test-token");
  });

  it("should not add Authorization header if access_token does not exist", async () => {
    server.use(
      http.get("https://localhost:7294/api/test-no-auth", ({ request }) => {
        return HttpResponse.json({
          hasToken: request.headers.has("Authorization"),
        });
      }),
    );

    const response = await axiosClient.get("/test-no-auth");
    expect(response.hasToken).toBe(false);
  });

  it("should unwrap response data on success", async () => {
    server.use(
      http.get("https://localhost:7294/api/test-unwrap", () => {
        return HttpResponse.json({ success: true, message: "Unwrapped!" });
      }),
    );

    const response = await axiosClient.get("/test-unwrap");
    expect(response).toEqual({ success: true, message: "Unwrapped!" });
  });

  it("should attempt to refresh token on 401 response and retry request", async () => {
    localStorage.setItem("access_token", "expired-token");
    localStorage.setItem("refresh_token", "valid-refresh");

    let requestCount = 0;

    server.use(
      // 1. The mocked protected endpoint that returns 401 first, then 200
      http.get("https://localhost:7294/api/protected", ({ request }) => {
        requestCount++;
        const auth = request.headers.get("Authorization");

        if (auth === "Bearer expired-token") {
          return new HttpResponse(null, { status: 401 });
        }

        if (auth === "Bearer new-valid-token") {
          return HttpResponse.json({ success: true, data: "Secured Data" });
        }

        return new HttpResponse(null, { status: 400 });
      }),

      // 2. The refresh token endpoint
      http.post(
        "https://localhost:7294/api/auth/refresh-token",
        async ({ request }) => {
          const body = await request.json();

          if (
            body.accessToken === "expired-token" &&
            body.refreshToken === "valid-refresh"
          ) {
            return HttpResponse.json({
              success: true,
              data: {
                accessToken: "new-valid-token",
                refreshToken: "new-valid-refresh",
              },
            });
          }
          return new HttpResponse(null, { status: 400 });
        },
      ),
    );

    const response = await axiosClient.get("/protected");

    expect(response).toEqual({ success: true, data: "Secured Data" });
    expect(requestCount).toBe(2); // One failure, one retry
    expect(localStorage.getItem("access_token")).toBe("new-valid-token");
    expect(localStorage.getItem("refresh_token")).toBe("new-valid-refresh");
  });

  it("should logout and redirect on refresh token failure", async () => {
    localStorage.setItem("access_token", "expired-token");
    localStorage.setItem("refresh_token", "invalid-refresh");
    localStorage.setItem("user", "{}");

    server.use(
      http.get("https://localhost:7294/api/protected-fail", () => {
        return new HttpResponse(null, { status: 401 });
      }),

      http.post("https://localhost:7294/api/auth/refresh-token", () => {
        return new HttpResponse(null, { status: 400 }); // Refresh fails
      }),
    );

    // Use fake timers to fast-forward the setTimeout redirect
    vi.useFakeTimers();

    await expect(axiosClient.get("/protected-fail")).rejects.toThrow();

    expect(localStorage.getItem("access_token")).toBeNull();
    expect(localStorage.getItem("refresh_token")).toBeNull();
    expect(localStorage.getItem("user")).toBeNull();

    // Fast-forward 1500ms for the redirect timeout
    vi.runAllTimers();

    expect(window.location.href).toBe("/login");

    vi.useRealTimers();
  });
});
