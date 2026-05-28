import useSWR from "swr";
import useSWRMutation from "swr/mutation";
import notificationApi from "../../api/notificationApi";
import { useAuth } from "../../contexts/auth-context";

/**
 * Hook: Lấy danh sách notifications với pagination
 * Vercel Rule: client-swr-dedup
 */
export const useNotificationList = (params = {}) => {
  const { pageNumber = 1, pageSize = 10, unreadOnly, type } = params;
  const { isAuthenticated } = useAuth();
  
  // Only query if authenticated
  const key = isAuthenticated 
    ? ["notifications", pageNumber, pageSize, unreadOnly, type]
    : null;

  const { data, error, isLoading, mutate } = useSWR(
    key,
    () => notificationApi.getAll({ pageNumber, pageSize, unreadOnly, type })
      .then((res) => res.data)
  );

  return {
    // Vercel Rule: rerender-derived-state
    notifications: data?.items || [],
    totalCount: data?.totalCount || 0,
    totalPages: data?.totalPages || 0,
    currentPage: data?.pageNumber || 1,
    isLoading,
    isError: error,
    mutate,
  };
};

/**
 * Hook: Lấy unread count
 */
export const useUnreadCount = () => {
  const { isAuthenticated } = useAuth();
  
  const { data, error, isLoading, mutate } = useSWR(
    isAuthenticated ? "notifications:unread-count" : null,
    () => notificationApi.getUnreadCount().then((res) => res.data),
    { 
      // Polling backup mỗi 60s in case SignalR drops and isn't caught
      refreshInterval: 60000 
    }
  );

  return {
    unreadCount: data ?? 0,
    isLoading,
    isError: error,
    mutate,
  };
};

/**
 * Hook: Mark notification as read (mutation)
 */
export const useMarkAsRead = () => {
  const { trigger, isMutating } = useSWRMutation(
    "notifications:mark-read",
    (_, { arg: id }) => notificationApi.markAsRead(id)
  );

  return { markAsRead: trigger, isMarking: isMutating };
};

/**
 * Hook: Mark all as read (mutation)
 */
export const useMarkAllAsRead = () => {
  const { trigger, isMutating } = useSWRMutation(
    "notifications:mark-all-read",
    () => notificationApi.markAllAsRead()
  );

  return { markAllAsRead: trigger, isMarking: isMutating };
};

/**
 * Hook: Delete notification (mutation)
 */
export const useDeleteNotification = () => {
  const { trigger, isMutating } = useSWRMutation(
    "notifications:delete",
    (_, { arg: id }) => notificationApi.delete(id)
  );

  return { deleteNotification: trigger, isDeleting: isMutating };
};
