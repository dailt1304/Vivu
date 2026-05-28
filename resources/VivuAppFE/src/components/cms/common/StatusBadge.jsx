import React from "react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";

const statusConfig = {
  // Blog/Location status
  pending: { label: "Chờ duyệt", dotColor: "bg-amber-500", bgColor: "bg-amber-50/50", textColor: "text-amber-700 border-amber-200/50" },
  approved: { label: "Đã duyệt", dotColor: "bg-emerald-500", isPulse: true, bgColor: "bg-emerald-50/50", textColor: "text-emerald-700 border-emerald-200/50" },
  published: { label: "Đã xuất bản", dotColor: "bg-emerald-500", isPulse: true, bgColor: "bg-emerald-50/50", textColor: "text-emerald-700 border-emerald-200/50" },
  rejected: { label: "Từ chối", dotColor: "bg-red-500", bgColor: "bg-red-50/50", textColor: "text-red-700 border-red-200/50" },
  draft: { label: "Nháp", dotColor: "bg-zinc-400", bgColor: "bg-zinc-100/50", textColor: "text-zinc-700 border-zinc-200/50" },
  hidden: { label: "Đã ẩn", dotColor: "bg-red-500", bgColor: "bg-red-50/50", textColor: "text-red-700 border-red-200/50" },

  // User status
  active: { label: "Hoạt động", dotColor: "bg-emerald-500", isPulse: true, bgColor: "bg-emerald-50/50", textColor: "text-emerald-700 border-emerald-200/50" },
  banned: { label: "Bị khóa", dotColor: "bg-red-500", bgColor: "bg-red-50/50", textColor: "text-red-700 border-red-200/50" },
  inactive: { label: "Không hoạt động", dotColor: "bg-zinc-400", bgColor: "bg-zinc-100/50", textColor: "text-zinc-700 border-zinc-200/50" },

  // Ticket status
  open: { label: "Mở", dotColor: "bg-amber-500", bgColor: "bg-amber-50/50", textColor: "text-amber-700 border-amber-200/50" },
  "in-progress": { label: "Đang xử lý", dotColor: "bg-blue-500", bgColor: "bg-blue-50/50", textColor: "text-blue-700 border-blue-200/50" },
  resolved: { label: "Đã giải quyết", dotColor: "bg-emerald-500", isPulse: true, bgColor: "bg-emerald-50/50", textColor: "text-emerald-700 border-emerald-200/50" },
  closed: { label: "Đã đóng", dotColor: "bg-zinc-400", bgColor: "bg-zinc-100/50", textColor: "text-zinc-700 border-zinc-200/50" },

  // Priority
  high: { label: "Cao", dotColor: "bg-red-500", bgColor: "bg-red-50/50", textColor: "text-red-700 border-red-200/50" },
  medium: { label: "Trung bình", dotColor: "bg-amber-500", bgColor: "bg-amber-50/50", textColor: "text-amber-700 border-amber-200/50" },
  low: { label: "Thấp", dotColor: "bg-zinc-400", bgColor: "bg-zinc-100/50", textColor: "text-zinc-700 border-zinc-200/50" },
};

const StatusBadge = ({ status, className }) => {
  const config = statusConfig[status?.toLowerCase()] || {
    label: status,
    dotColor: "bg-zinc-400",
    bgColor: "bg-zinc-100",
    textColor: "text-zinc-700"
  };

  return (
    <div className={cn(
      "inline-flex items-center gap-1.5 px-2.5 py-0.5 text-[11px] font-medium tracking-wide border rounded-md transition-colors",
      config.bgColor,
      config.textColor,
      className
    )}>
      <span className={cn("relative flex h-1.5 w-1.5")}>
        {config.isPulse && (
          <span className={cn("animate-ping absolute inline-flex h-full w-full rounded-full opacity-75", config.dotColor)} style={{ animationDuration: '2s' }}></span>
        )}
        <span className={cn("relative inline-flex rounded-full h-1.5 w-1.5", config.dotColor)}></span>
      </span>
      {config.label}
    </div>
  );
};

export default StatusBadge;
