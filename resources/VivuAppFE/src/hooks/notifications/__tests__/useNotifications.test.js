import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { server } from "../../../test/mocks/server";
import { useNotificationList, useUnreadCount, useMarkAsRead, useMarkAllAsRead, useDeleteNotification } from "../useNotifications";

const API_URL = "https://localhost:7294/api";

// Mock useAuth
vi.mock("../../auth-context", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    userId: "user-123"
  })
}));

describe("useNotifications Hook", () => {
  beforeEach(() => {
    server.resetHandlers();
  });

  afterEach(() => {
    server.restoreHandlers();
  });

  describe("useNotificationList", () => {
    it("should fetch notifications successfully", async () => {
      const mockData = {
        items: [{ id: "1", title: "Test Notification" }],
        totalCount: 1,
        totalPages: 1,
        pageNumber: 1
      };

      server.use(
        http.get(`${API_URL}/notifications`, () => {
          return HttpResponse.json({ success: true, data: mockData });
        })
      );

      const { result } = renderHook(() => useNotificationList());

      await waitFor(() => expect(result.current.isLoading).toBe(false));

      expect(result.current.notifications).toHaveLength(1);
      expect(result.current.notifications[0].title).toBe("Test Notification");
      expect(result.current.totalCount).toBe(1);
    });

    it("should handle error when fetching notifications", async () => {
      server.use(
        http.get(`${API_URL}/notifications`, () => {
          return new HttpResponse(null, { status: 500 });
        })
      );

      const { result } = renderHook(() => useNotificationList());

      await waitFor(() => expect(result.current.isLoading).toBe(false));
      expect(result.current.isError).toBeDefined();
    });
  });

  describe("useUnreadCount", () => {
    it("should fetch unread count successfully", async () => {
      server.use(
        http.get(`${API_URL}/notifications/unread-count`, () => {
          return HttpResponse.json({ success: true, data: 5 });
        })
      );

      const { result } = renderHook(() => useUnreadCount());

      await waitFor(() => expect(result.current.isLoading).toBe(false));
      expect(result.current.unreadCount).toBe(5);
    });
  });

  describe("useMarkAsRead", () => {
    it("should trigger markAsRead mutation", async () => {
      let capturedId = null;
      server.use(
        http.patch(`${API_URL}/notifications/:id/read`, ({ params }) => {
          capturedId = params.id;
          return HttpResponse.json({ success: true });
        })
      );

      const { result } = renderHook(() => useMarkAsRead());
      
      await result.current.markAsRead("notif-1");
      
      expect(capturedId).toBe("notif-1");
    });
  });

  describe("useMarkAllAsRead", () => {
    it("should trigger markAllAsRead mutation", async () => {
      let called = false;
      server.use(
        http.patch(`${API_URL}/notifications/read-all`, () => {
          called = true;
          return HttpResponse.json({ success: true });
        })
      );

      const { result } = renderHook(() => useMarkAllAsRead());
      
      await result.current.markAllAsRead();
      
      expect(called).toBe(true);
    });
  });

  describe("useDeleteNotification", () => {
    it("should trigger delete mutation", async () => {
      let capturedId = null;
      server.use(
        http.delete(`${API_URL}/notifications/:id`, ({ params }) => {
          capturedId = params.id;
          return HttpResponse.json({ success: true });
        })
      );

      const { result } = renderHook(() => useDeleteNotification());
      
      await result.current.deleteNotification("notif-delete-1");
      
      expect(capturedId).toBe("notif-delete-1");
    });
  });
});
