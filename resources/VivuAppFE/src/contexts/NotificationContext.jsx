import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import { NotificationContext } from "./notification-context";
import { useAuth } from "./auth-context";
import { createNotificationConnection } from "../services/signalr";
import notificationApi from "../api/notificationApi";

/**
 * NotificationProvider - Single source of truth for notification state
 * Manages SignalR connection, unread count, and real-time updates.
 */
export function NotificationProvider({ children }) {
  const { isAuthenticated } = useAuth();
  const [unreadCount, setUnreadCount] = useState(0);
  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef(null);

  // Helper to get fresh token without triggering re-renders
  const getAccessToken = () => localStorage.getItem("access_token");

  // Fetch initial unread count
  const fetchUnreadCount = useCallback(async () => {
    if (!isAuthenticated) return;
    try {
      const response = await notificationApi.getUnreadCount();
      // Depending on API response wrapper: assuming response.data holds the actual value
      // Adjust based on your API's standard response structure
      if (response && response.success !== false) {
          setUnreadCount(response.data !== undefined ? response.data : response);
      }
    } catch (error) {
      console.error("Failed to fetch unread count:", error);
    }
  }, [isAuthenticated]);

  // SignalR Lifecycle & Hub connection
  useEffect(() => {
    // Vercel Rule: js-early-exit
    if (!isAuthenticated) {
      setUnreadCount(0);
      setIsConnected(false);
      return;
    }

    let isMounted = true;
    const token = getAccessToken();
    if (!token) return;

    // Vercel Rule: async-parallel idea: we don't await fetch before connecting
    fetchUnreadCount();

    const newConnection = createNotificationConnection(token);

    newConnection
      .start()
      .then(() => {
        if (!isMounted) return;
        setIsConnected(true);
        connectionRef.current = newConnection;

        // Lắng nghe event ReceiveNotification từ backend
        newConnection.on("ReceiveNotification", () => {
          // Vercel Rule: rerender-functional-setstate (Update optimistic)
          setUnreadCount((prev) => prev + 1);
        });

        // Lắng nghe event cập nhật trực tiếp unread count
        newConnection.on("UnreadCountUpdated", (count) => {
          setUnreadCount(count);
        });
      })
      .catch((e) => {
        console.error("Notification SignalR Connection failed: ", e);
      });

    // Cleanup on unmount or logout
    return () => {
      isMounted = false;
      newConnection.off("ReceiveNotification");
      newConnection.off("UnreadCountUpdated");
      newConnection.stop();
      setIsConnected(false);
      connectionRef.current = null;
    };
  }, [isAuthenticated, fetchUnreadCount]);

  // Actions
  const markAsRead = useCallback(async (id) => {
    try {
      await notificationApi.markAsRead(id);
      // Optimistic update
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (error) {
      console.error("Failed to mark notification as read", error);
      throw error;
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await notificationApi.markAllAsRead();
      // Optimistic update
      setUnreadCount(0);
    } catch (error) {
      console.error("Failed to mark all notifications as read", error);
      throw error;
    }
  }, []);

  const deleteNotification = useCallback(
    async (id) => {
      try {
        await notificationApi.delete(id);
        // After delete, the unread count might have changed if the deleted one was unread.
        // Easiest and safest way is to re-fetch to sync with backend.
        fetchUnreadCount();
      } catch (error) {
        console.error("Failed to delete notification", error);
        throw error;
      }
    },
    [fetchUnreadCount],
  );

  const refetchUnreadCount = useCallback(() => {
    fetchUnreadCount();
  }, [fetchUnreadCount]);

  // Vercel Rule: rerender-memo
  const value = useMemo(
    () => ({
      unreadCount,
      isConnected,
      markAsRead,
      markAllAsRead,
      deleteNotification,
      refetchUnreadCount,
    }),
    [
      unreadCount,
      isConnected,
      markAsRead,
      markAllAsRead,
      deleteNotification,
      refetchUnreadCount,
    ],
  );

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
}

export default NotificationContext;
