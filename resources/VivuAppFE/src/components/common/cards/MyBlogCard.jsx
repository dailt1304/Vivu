import React, { memo } from "react";
import {
  Eye,
  Heart,
  MessageCircle,
  Pencil,
  Trash2,
  Rocket,
  Calendar,
} from "lucide-react";

// Vercel Rule: rendering-hoist-jsx — static config outside component
const STATUS_CONFIG = {
  published: {
    label: "Đã đăng",
    className: "bg-emerald-100 text-emerald-700 border-emerald-200",
  },
  draft: {
    label: "Bản nháp",
    className: "bg-amber-100 text-amber-700 border-amber-200",
  },
};

/**
 * Landscape blog card for My Blogs page with action buttons.
 * Vercel Rules: rerender-memo, rendering-conditional-render, js-early-exit
 */
const MyBlogCard = memo(
  ({ data, onEdit, onDelete, onPublish, onView, isPublishing }) => {
    // Vercel Rule: js-early-exit
    if (!data) return null;

    const isPublished = !!data.publishedAt;
    const status = isPublished ? STATUS_CONFIG.published : STATUS_CONFIG.draft;
    const dateStr = data.publishedAt
      ? new Date(data.publishedAt).toLocaleDateString("vi-VN")
      : data.createdAt
        ? new Date(data.createdAt).toLocaleDateString("vi-VN")
        : "N/A";

    return (
      <div
        onClick={() => onView?.(data)}
        className="group relative flex flex-col tap-highlight-transparent cursor-pointer h-full"
        style={{ contain: "content" }}
      >
        {/* Image Container */}
        <div className="relative w-full aspect-[4/3] sm:aspect-[4/5] rounded-[20px] overflow-hidden bg-gray-100 flex-shrink-0 isolate">
          {data.coverImageUrl ? (
            <img
              src={data.coverImageUrl}
              alt={data.title}
              loading="lazy"
              className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-gray-300 text-4xl">
              📝
            </div>
          )}
          <div className="absolute inset-0 bg-black/5 group-hover:bg-black/0 transition-colors duration-300" />
          
          {/* Top Left: Status Badge */}
          <div className="absolute top-2.5 left-2.5 z-10">
            <span
              className={`px-2.5 py-1 rounded-full text-[10px] font-bold shadow-sm backdrop-blur-md border border-white/20 ${
                isPublished 
                  ? "bg-emerald-500/80 text-white" 
                  : "bg-amber-500/80 text-white"
              }`}
            >
              {status.label}
            </span>
          </div>

          {/* Action Overlay: Edit & Delete */}
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-2 opacity-0 group-hover:opacity-100 bg-black/40 backdrop-blur-[2px] transition-all duration-300">
            <button
              onClick={(e) => {
                e.stopPropagation();
                onEdit?.(data);
              }}
              className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-white bg-gradient-primary hover:opacity-90 rounded-full transition-all active:scale-95 shadow-lg w-32 justify-center"
            >
              <Pencil size={16} />
              Sửa
            </button>

            <button
              onClick={(e) => {
                e.stopPropagation();
                onDelete?.(data);
              }}
              className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-white bg-red-600/90 hover:bg-red-600 rounded-full transition-all active:scale-95 shadow-lg w-32 justify-center"
            >
              <Trash2 size={16} />
              Xóa
            </button>
          </div>

          {/* Bottom Right: Stats Pills (Only if published) */}
          {isPublished && (
            <div className="absolute bottom-2.5 right-2.5 left-2.5 z-10 flex flex-wrap justify-end items-center gap-1.5 pointer-events-none">
              <div className="bg-black/40 backdrop-blur-md px-2 py-1 rounded-md text-[10px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Lượt xem">
                <Eye size={12} /> <span>{data.viewCount || 0}</span>
              </div>
              <div className="bg-black/40 backdrop-blur-md px-2 py-1 rounded-md text-[10px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Lượt thích">
                <Heart size={12} /> <span>{data.likeCount || 0}</span>
              </div>
            </div>
          )}
        </div>

        {/* Content Section (Below Image) */}
        <div className="pt-2.5 pb-0 flex flex-col justify-between flex-1">
          <div>
            <h3 className="font-bold text-sm sm:text-base text-gray-900 line-clamp-2 leading-snug group-hover:text-blue-600 transition-colors">
              {data.title}
            </h3>
            <p className="text-xs text-gray-400 font-medium mt-1">
              {dateStr}
            </p>
          </div>
        </div>
      </div>
    );
  },
);

MyBlogCard.displayName = "MyBlogCard";
export default MyBlogCard;
