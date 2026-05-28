import React, { memo } from "react";
import { Heart, Bookmark, MessageCircle, Flag } from "lucide-react";
import { motion } from "framer-motion";

/**
 * Editorial Floating Action Bar
 * Desktop: Fixed bottom center bar
 * Mobile: Fixed bottom bar
 * Vercel Rule: rerender-memo
 */
const BlogFloatingActions = memo(
  ({
    likeCount,
    isLiked,
    onToggleLike,
    commentCount,
    onNavigateComments,
    saveCount,
    isSaved,
    onToggleSave,

    onReport,
    isAuthor = false,
  }) => {
    return (
      <>
        {/* Desktop Fixed Bottom Bar */}
        <div className="hidden lg:flex fixed bottom-8 left-1/2 -translate-x-1/2 z-40 items-center justify-center gap-6 bg-white/90 backdrop-blur-xl border border-gray-100 rounded-full py-3 px-8 shadow-[0_10px_40px_rgb(0,0,0,0.08)]">
          <ActionButton
            icon={Heart}
            count={likeCount}
            isActive={isLiked}
            onClick={onToggleLike}
            activeColor="text-red-500"
            activeBg="bg-red-50"
            isMobile
          />
          <ActionButton
            icon={MessageCircle}
            count={commentCount}
            onClick={onNavigateComments}
            isMobile
          />
          <ActionButton
            icon={Bookmark}
            count={saveCount}
            isActive={isSaved}
            onClick={onToggleSave}
            activeColor="text-blue-500"
            activeBg="bg-blue-50"
            isMobile
          />
          <div className="h-6 w-px bg-gray-200 mx-1" />
          {!isAuthor && onReport && (
            <ActionButton
              icon={Flag}
              onClick={onReport}
              isMobile
              hoverColor="text-red-500"
              hoverBg="hover:bg-red-50"
            />
          )}
        </div>

        {/* Mobile Fixed Bottom Bar */}
        <div className="lg:hidden fixed bottom-0 left-0 right-0 z-50 bg-white/90 backdrop-blur-xl border-t border-gray-200 shadow-[0_-4px_20px_rgb(0,0,0,0.05)] pb-safe-bottom">
          <div className="flex items-center justify-around py-3 px-4">
            <ActionButton
              icon={Heart}
              count={likeCount}
              isActive={isLiked}
              onClick={onToggleLike}
              activeColor="text-red-500"
              activeBg="bg-red-50"
              isMobile
            />
            <ActionButton
              icon={MessageCircle}
              count={commentCount}
              onClick={onNavigateComments}
              isMobile
            />
            <ActionButton
              icon={Bookmark}
              count={saveCount}
              isActive={isSaved}
              onClick={onToggleSave}
              activeColor="text-blue-500"
              activeBg="bg-blue-50"
              isMobile
            />

            {!isAuthor && onReport && (
              <ActionButton
                icon={Flag}
                onClick={onReport}
                isMobile
                hoverColor="text-red-500"
                hoverBg="hover:bg-red-50"
              />
            )}
          </div>
        </div>
      </>
    );
  },
);

// Reusable action button component
const ActionButton = memo(
  ({
    icon: Icon,
    count,
    isActive,
    onClick,
    activeColor = "text-gray-900",
    activeBg = "bg-gray-100",
    hoverColor,
    hoverBg,
    isMobile = false,
  }) => {
    return (
      <button
        onClick={onClick}
        className={`relative group flex ${isMobile ? "flex-row gap-2" : "flex-col"} items-center justify-center`}
      >
        <motion.div
          whileTap={{ scale: 0.9 }}
          className={`p-3 rounded-full transition-all duration-300 ${
            isActive
              ? `${activeColor} ${activeBg}`
              : `text-gray-500 ${hoverColor ? `hover:${hoverColor}` : "hover:text-gray-900"} ${hoverBg || "hover:bg-gray-100"}`
          }`}
        >
          <Icon
            size={20}
            className={isActive ? "fill-current" : ""}
            strokeWidth={isActive ? 2.5 : 2}
          />
        </motion.div>
        {count !== undefined && (
          <span
            className={`text-[11px] font-bold ${isActive ? activeColor : "text-gray-500"} ${isMobile ? "" : "mt-1"}`}
          >
            {count > 999 ? "999+" : count}
          </span>
        )}
      </button>
    );
  },
);

BlogFloatingActions.displayName = "BlogFloatingActions";
ActionButton.displayName = "ActionButton";

export default BlogFloatingActions;
