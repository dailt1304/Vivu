import React from "react";
import { formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";
import { Users, MessageCircle, Heart, CreditCard, Zap, Info } from "lucide-react";

/**
 * Single notification item component
 * Vercel Rule: rendering-hoist-jsx (icon map is hoisted)
 */
const ICON_MAP = {
  MEMBER_JOINED: Users,
  MEMBER_LEFT: Users,
  MEMBER_REMOVED: Users,
  NEW_COMMENT: MessageCircle,
  NEW_LIKE: Heart,
  PAYMENT_SUCCESS: CreditCard,
  PAYMENT_FAILED: CreditCard,
  AI_QUOTA_LOW: Zap,
  AI_QUOTA_EXCEEDED: Zap,
  DEFAULT: Info
};

const COLOR_MAP = {
  MEMBER_JOINED: "text-blue-500 bg-blue-100",
  MEMBER_LEFT: "text-gray-500 bg-gray-100",
  MEMBER_REMOVED: "text-red-500 bg-red-100",
  NEW_COMMENT: "text-green-500 bg-green-100",
  NEW_LIKE: "text-pink-500 bg-pink-100",
  PAYMENT_SUCCESS: "text-emerald-500 bg-emerald-100",
  PAYMENT_FAILED: "text-red-500 bg-red-100",
  AI_QUOTA_LOW: "text-amber-500 bg-amber-100",
  AI_QUOTA_EXCEEDED: "text-orange-500 bg-orange-100",
  DEFAULT: "text-blue-500 bg-blue-100"
};

const NotificationItem = React.memo(({ notification, onClick, onClose }) => {
  const isUnread = !notification.isRead;
  const Icon = ICON_MAP[notification.type] || ICON_MAP.DEFAULT;
  const colorClass = COLOR_MAP[notification.type] || COLOR_MAP.DEFAULT;

  const handleClick = (e) => {
    e.preventDefault();
    if (onClick) onClick(notification);
    if (onClose) onClose();
  };

  const timeAgo = notification.createdAt 
    ? formatDistanceToNow(new Date(notification.createdAt), { addSuffix: true, locale: vi })
    : "Vừa xong";

  return (
    <button
      onClick={handleClick}
      className={`w-full text-left p-4 flex items-start gap-4 hover:bg-gray-50 transition-colors border-b border-gray-100 last:border-b-0
        ${isUnread ? "bg-blue-50/30" : "bg-white"}
      `}
    >
      <div className={`mt-1 flex-shrink-0 p-2 rounded-full ${colorClass}`}>
        <Icon size={18} />
      </div>
      
      <div className="flex-1 min-w-0">
        <p className={`text-sm ${isUnread ? "font-semibold text-gray-900" : "font-medium text-gray-800"}`}>
          {notification.title}
        </p>
        {notification.content && (
          <p className="text-sm text-gray-500 mt-1 line-clamp-2">
            {notification.content}
          </p>
        )}
        <p className="text-xs text-gray-400 mt-2">
          {timeAgo}
        </p>
      </div>

      {isUnread && (
        <div className="flex-shrink-0 mt-2">
          <div className="w-2.5 h-2.5 bg-blue-500 rounded-full"></div>
        </div>
      )}
    </button>
  );
});

NotificationItem.displayName = "NotificationItem";

export default NotificationItem;
