import React from "react";
import { Bot, User } from "lucide-react";

const MentionSuggestions = ({
  visible,
  filterText,
  members,
  onSelect,
  onClose,
}) => {
  if (!visible) return null;

  // Filter functionality
  const suggestions = [
    { id: "vivuAI", name: "VivuAI", type: "ai", avatar: null },
    ...members.map((m) => {
      // Handle both flattened and nested structure
      const user = m.user || {
        id: m.userId,
        fullName: m.fullName,
        avatarUrl: m.avatarUrl,
      };

      return {
        id: user.id,
        name: user.fullName || "Unnamed User",
        type: "member",
        avatar: user.avatarUrl,
      };
    }),
  ].filter((item) =>
    item.name.toLowerCase().includes(filterText.toLowerCase()),
  );

  if (suggestions.length === 0) return null;

  return (
    <div className="absolute bottom-full left-0 mb-2 w-64 bg-white/95 backdrop-blur-md border border-gray-200/80 rounded-xl shadow-xl shadow-gray-200/50 overflow-hidden z-50 animate-in fade-in slide-in-from-bottom-2 duration-200">
      <div className="px-3 py-2 border-b border-gray-100 text-xs font-semibold text-gray-400 uppercase tracking-wider">
        Gợi ý nhắc tên
      </div>
      <div className="max-h-48 overflow-y-auto py-1">
        {suggestions.map((item, index) => (
          <button
            key={item.id}
            onClick={() => onSelect(item)}
            className="w-full flex items-center gap-3 px-3 py-2 ho:bg-blue-50/50 hover:bg-slate-50 transition-colors text-left group"
          >
            {/* Avatar / Icon */}
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 shadow-sm ${
                item.type === "ai"
                  ? "bg-gradient-primary text-white"
                  : "bg-white border border-gray-100"
              }`}
            >
              {item.type === "ai" ? (
                <Bot size={16} />
              ) : item.avatar ? (
                <img
                  src={item.avatar}
                  alt={item.name}
                  className="w-full h-full rounded-full object-cover"
                />
              ) : (
                <User size={16} className="text-gray-400" />
              )}
            </div>

            <div className="flex flex-col min-w-0">
              <span
                className={`text-sm font-medium truncate ${
                  item.type === "ai" ? "text-blue-600" : "text-gray-700"
                }`}
              >
                {item.name}
              </span>
              <span className="text-[10px] text-gray-400">
                {item.type === "ai" ? "Trợ lý ảo" : "Thành viên"}
              </span>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};

export default MentionSuggestions;
