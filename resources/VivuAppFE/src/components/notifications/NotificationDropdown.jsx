import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useNotifications } from "../../contexts/notification-context";
import { useNotificationList, useMarkAsRead, useMarkAllAsRead } from "../../hooks/notifications/useNotifications";
import NotificationItem from "./NotificationItem";
import { Check, Bell, Loader2 } from "lucide-react";

/**
 * Shared Notification List logic for both Dropdown (Desktop) and BottomSheet (Mobile)
 */
export const NotificationListContent = ({ onClose }) => {
  const [filter, setFilter] = useState("all"); // 'all' | 'unread'
  const { unreadCount, markAsRead: contextMarkAsRead, refetchUnreadCount } = useNotifications();
  
  // Data fetching
  const { notifications, isLoading, mutate } = useNotificationList({
    pageSize: 20,
    unreadOnly: filter === "unread" ? true : undefined
  });

  const { markAllAsRead, isMarking } = useMarkAllAsRead();
  const navigate = useNavigate();

  const handleMarkAllRead = async () => {
    try {
      await markAllAsRead();
      mutate();
      refetchUnreadCount();
    } catch (e) {
      console.error("Failed to mark all as read: ", e);
    }
  };

  const handleNotificationClick = async (notification) => {
    // 1. Mark as read optimistically if unread
    if (!notification.isRead) {
      try {
        // Use Context markAsRead to update global unreadCount (Vercel: state-decouple-implementation)
        await contextMarkAsRead(notification.id);
        
        // Update local SWR list cache (Vercel: client-swr-mutate)
        mutate(currentData => {
           if (!currentData) return currentData;
           
           if (filter === "unread") {
             // Real-time: Remove from list if in 'unread' tab
             return {
               ...currentData,
               items: currentData.items.filter(n => n.id !== notification.id)
             };
           }
           
           // Update state if in 'all' tab
           return {
              ...currentData,
              items: currentData.items.map(n => n.id === notification.id ? { ...n, isRead: true } : n)
           };
        }, false);
        
        // Final sync: refetch count in background
        refetchUnreadCount();
      } catch (e) {
        console.error("Error marking as read:", e);
      }
    }

    // 2. Navigate based on confirmed routing
    // MEMBER_JOINED / MEMBER_LEFT / MEMBER_REMOVED → /trips/{referenceId}
    // NEW_COMMENT / NEW_LIKE → /inspiration/blog/{referenceId}
    // PAYMENT_SUCCESS / PAYMENT_FAILED → /profile?tab=payment
    // AI_QUOTA_LOW / AI_QUOTA_EXCEEDED → /subscription
    
    const type = notification.type;
    const refId = notification.referenceId;
    
    if (type.startsWith("MEMBER_")) {
       navigate(`/trips/${refId}`);
    } else if (type === "NEW_COMMENT" || type === "NEW_LIKE") {
       navigate(`/inspiration/${refId}`);
    } else if (type.startsWith("PAYMENT_")) {
       navigate("/profile?tab=payment");
    } else if (type.startsWith("AI_QUOTA_")) {
       navigate("/subscription");
    }

    // 3. Close dropdown/sheet
    if (onClose) onClose();
  };

  return (
    <div className="flex flex-col h-full max-h-[480px] sm:max-h-[60vh] md:max-h-[480px]">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100 shrink-0 sticky top-0 bg-white z-10 rounded-t-xl">
        <h3 className="font-semibold text-gray-900 text-lg sm:text-base">Thông báo</h3>
        {unreadCount > 0 && (
          <button
            onClick={handleMarkAllRead}
            disabled={isMarking}
            className="text-xs font-medium text-blue-600 hover:text-blue-700 flex items-center gap-1 transition-colors outline-none"
          >
            {isMarking ? <Loader2 size={14} className="animate-spin" /> : <Check size={14} />}
            Đánh dấu tất cả đã đọc
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="flex gap-4 px-4 py-2 border-b border-gray-50 shrink-0 bg-white">
        <button
          onClick={() => setFilter("all")}
          className={`text-sm font-medium transition-colors outline-none relative pb-2 ${filter === "all" ? "text-blue-600" : "text-gray-500 hover:text-gray-800"}`}
        >
          Tất cả
          {filter === "all" && <span className="absolute bottom-0 left-0 w-full h-[2px] bg-blue-600 rounded-t-md" />}
        </button>
        <button
          onClick={() => setFilter("unread")}
          className={`text-sm font-medium transition-colors outline-none relative pb-2 flex items-center gap-1.5 ${filter === "unread" ? "text-blue-600" : "text-gray-500 hover:text-gray-800"}`}
        >
          Chưa đọc
          {unreadCount > 0 && (
            <span className={`px-1.5 py-0.5 rounded-full text-[10px] ${filter === "unread" ? "bg-blue-100 text-blue-700" : "bg-gray-100 text-gray-600"}`}>
              {unreadCount}
            </span>
          )}
          {filter === "unread" && <span className="absolute bottom-0 left-0 w-full h-[2px] bg-blue-600 rounded-t-md" />}
        </button>
      </div>

      {/* List */}
      <div className="flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center p-8 text-gray-400">
             <Loader2 size={24} className="animate-spin mb-2" />
             <p className="text-sm">Đang tải...</p>
          </div>
        ) : notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center p-8 text-center bg-gray-50/50 h-full">
            <div className="w-12 h-12 bg-gray-100 rounded-full flex items-center justify-center mb-3">
              <Bell size={20} className="text-gray-400" />
            </div>
            <p className="font-medium text-gray-700">Không có thông báo nào</p>
            <p className="text-sm text-gray-500 mt-1">Khi bạn có thông báo mới, chúng sẽ xuất hiện ở đây.</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-50">
            {notifications.map((notif) => (
              <NotificationItem
                key={notif.id}
                notification={notif}
                onClick={handleNotificationClick}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

/**
 * Desktop Dropdown Component
 */
const NotificationDropdown = ({ isOpen, onClose }) => {
  if (!isOpen) return null;

  return (
    <div className="absolute right-0 top-full mt-2 w-[400px] bg-white rounded-xl shadow-lg border border-gray-100 z-50 overflow-hidden transform origin-top-right transition-all">
       <NotificationListContent onClose={onClose} />
    </div>
  );
};

export default NotificationDropdown;
