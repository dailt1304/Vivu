import React from "react";
import { X, Share2, Maximize2, Minimize2 } from "lucide-react";

/**
 * TripDrawerTopBar - Top navigation bar for TripDetailsDrawer
 *
 * Contains close button, member avatars, share button, and expand/collapse toggle.
 */
function TripDrawerTopBar({
  onClose,
  onShareClick,
  isExpanded,
  onToggleExpand,
  ownerName,
  members = [],
}) {
  return (
    <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100">
      {/* Left: Close Button */}
      <div className="flex items-center gap-2">
        <button
          className="p-2 hover:bg-slate-100 rounded-full transition-colors"
          onClick={onClose}
          aria-label="Đóng"
        >
          <X size={24} className="text-slate-500" />
        </button>
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-2">

        {/* Share Button */}
        <button
          onClick={onShareClick}
          className="p-2 bg-slate-50 text-slate-600 rounded-full hover:bg-slate-100 transition-colors"
          title="Chia sẻ & Mời thành viên"
          aria-label="Chia sẻ"
        >
          <Share2 size={18} />
        </button>

        {/* Toggle Expansion Button */}
        <button
          onClick={onToggleExpand}
          className="p-2 bg-slate-50 text-slate-600 rounded-full hover:bg-slate-100 transition-colors hidden md:block"
          title={isExpanded ? "Thu nhỏ" : "Mở rộng"}
          aria-label={isExpanded ? "Thu nhỏ" : "Mở rộng"}
        >
          {isExpanded ? <Minimize2 size={18} /> : <Maximize2 size={18} />}
        </button>
      </div>
    </div>
  );
}

export default React.memo(TripDrawerTopBar);
