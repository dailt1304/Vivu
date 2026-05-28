import React from "react";
import { Sparkles } from "lucide-react";

const ChatSkeleton = () => {
  return (
    <div className="flex gap-3 animate-in fade-in duration-300">
      {/* Avatar */}
      <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-blue-500 to-cyan-500 flex items-center justify-center text-white shadow-sm shrink-0">
        <Sparkles size={16} className="animate-pulse" />
      </div>

      {/* Content Skeleton */}
      <div className="flex-1 space-y-3 max-w-[80%]">
        <div className="bg-white border border-gray-100 rounded-2xl rounded-bl-sm p-4 shadow-sm">
          {/* Animated lines */}
          <div className="space-y-2">
            <div className="h-4 bg-gray-200 rounded-full animate-pulse w-3/4"></div>
            <div className="h-4 bg-gray-200 rounded-full animate-pulse w-full"></div>
            <div className="h-4 bg-gray-200 rounded-full animate-pulse w-5/6"></div>
            <div className="h-4 bg-gray-200 rounded-full animate-pulse w-2/3"></div>
          </div>

          {/* Shimmer effect */}
          <div className="mt-4 flex items-center gap-2 text-sm text-blue-500 font-medium">
            <div className="w-4 h-4 rounded-full bg-blue-100 animate-pulse"></div>
            <span className="animate-pulse">Đang tạo lịch trình...</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ChatSkeleton;
