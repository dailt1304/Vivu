import React, { memo, useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { User, MessageCircle, Clock, Heart, Bookmark, Eye } from "lucide-react";
import { motion as Motion } from "framer-motion";
import { useLikeBlog, useBookmarkBlog } from "../../../hooks/blogs/useBlogs";
import { useAuth } from "../../../contexts/auth-context";
import toast from "../../../utils/toast";
import { mutate } from "swr";

/**
 * Magazine Cover style for Blogs
 * Immersive overlay, dark gradient on bottom, white typography.
 * Portrait aspect 3:4.
 */
const BlogCard = memo(({ data, variants, className = "", large = false, compact = false }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { trigger: likeBlog } = useLikeBlog();
  const { trigger: bookmarkBlog } = useBookmarkBlog();

  // Local state for immediate UI feedback
  const [isLiked, setIsLiked] = useState(
    !!(data.isLikedByCurrentUser || data.isLiked || data.isLike || data.liked),
  );
  const [isBookmarked, setIsBookmarked] = useState(
    !!(data.isBookmarkedByCurrentUser || data.isBookmarked),
  );

  // Sync back if data changes (SWR revalidation)
  useEffect(() => {
    setIsLiked(!!(data.isLikedByCurrentUser || data.isLiked || data.isLike || data.liked));
    setIsBookmarked(!!(data.isBookmarkedByCurrentUser || data.isBookmarked));
  }, [data.isLikedByCurrentUser, data.isLiked, data.isLike, data.liked, data.isBookmarkedByCurrentUser, data.isBookmarked]);

  const handleCardClick = () => {
    navigate(`/inspiration/${data.id}`);
  };

  const handleLike = async (e) => {
    e.stopPropagation();
    if (!user) {
      toast.info("Vui lòng đăng nhập để thích bài viết");
      return;
    }

    // Phản hồi tức thì trên UI
    const newStatus = !isLiked;
    setIsLiked(newStatus);

    try {
      await likeBlog(data.id);
      toast.success(newStatus ? "Đã thích bài viết" : "Đã bỏ thích bài viết");
      // Mutate để đồng bộ cache ngầm
      mutate((key) => Array.isArray(key) && key[0] === "public-blogs");
      mutate(["blog", data.id]);
    } catch {
      // Rollback nếu API lỗi
      setIsLiked(!newStatus);
      toast.error("Không thể cập nhật lượt thích");
    }
  };

  const handleBookmark = async (e) => {
    e.stopPropagation();
    if (!user) {
      toast.info("Vui lòng đăng nhập để lưu bài viết");
      return;
    }

    const newStatus = !isBookmarked;
    setIsBookmarked(newStatus);

    try {
      await bookmarkBlog(data.id);
      toast.success(
        newStatus ? "Đã lưu bài viết" : "Đã bỏ lưu bài viết",
      );
      mutate((key) => Array.isArray(key) && key[0] === "public-blogs");
      mutate((key) => Array.isArray(key) && key[0] === "my-bookmarks");
      mutate(["blog", data.id]);
    } catch {
      setIsBookmarked(!newStatus);
      toast.error("Lỗi khi cập nhật bookmark");
    }
  };

  if (!data) return null;

  return (
    <Motion.div
      variants={variants}
      onClick={handleCardClick}
      className={`group cursor-pointer w-full flex flex-col ${className}`}
    >
      {/* 1. Thumbnail Container */}
      <div className={`relative w-full rounded-2xl overflow-hidden shadow-sm transition-all duration-300 group-hover:shadow-md ${
        large ? "aspect-[4/3] sm:aspect-video" : "aspect-[4/5] sm:aspect-square"
      }`}>
        <img
          src={data.coverImageUrl}
          alt={data.title}
          loading="lazy"
          className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
        />
        <div className="absolute inset-0 bg-black/5 group-hover:bg-black/0 transition-colors duration-300" />
        
        {/* Top Left: Author Pill */}
        <div className="absolute top-2.5 left-2.5 z-10 max-w-[calc(100%-80px)]">
          <div className="bg-black/40 backdrop-blur-md pr-2 pl-1 py-1 rounded-full text-[11px] font-medium text-white flex items-center gap-1.5 shadow-sm border border-white/10 transition-colors hover:bg-black/50 w-full">
            <div className="w-5 h-5 rounded-full bg-gray-200 overflow-hidden shrink-0 border border-white/20">
              {data.authorAvatarUrl ? (
                <img src={data.authorAvatarUrl} alt={data.authorName} className="w-full h-full object-cover" />
              ) : (
                <User size={12} className="w-full h-full p-0.5 text-gray-500 bg-white" />
              )}
            </div>
            <span className="truncate drop-shadow-sm min-w-0">{data.authorName || "Người dùng"}</span>
          </div>
        </div>

        {/* Top Right Badges: Like & Bookmark */}
        <div className="absolute top-2.5 right-2.5 z-10 flex items-center gap-1.5">
          <button
            onClick={handleBookmark}
            className={`p-1.5 rounded-full transition-all active:scale-95 shadow-sm border border-white/10 ${
              isBookmarked
                ? "bg-blue-500 text-white border-transparent"
                : "bg-black/40 backdrop-blur-md text-white hover:bg-black/60"
            }`}
            title={isBookmarked ? "Bỏ lưu" : "Lưu bài viết"}
          >
            <Bookmark
              size={14}
              fill={isBookmarked ? "currentColor" : "none"}
            />
          </button>
          <button
            onClick={handleLike}
            className={`p-1.5 rounded-full transition-all active:scale-95 shadow-sm border border-white/10 ${
              isLiked
                ? "bg-red-500 text-white border-transparent"
                : "bg-black/40 backdrop-blur-md text-white hover:bg-black/60"
            }`}
            title={isLiked ? "Bỏ thích" : "Thích bài viết"}
          >
            <Heart
              size={14}
              fill={isLiked ? "currentColor" : "none"}
            />
          </button>
        </div>

        {/* Bottom Right: Stats Pills */}
        <div className="absolute bottom-2.5 right-2.5 left-2.5 z-10 flex flex-wrap justify-end items-center gap-1">
           <div className="bg-black/40 backdrop-blur-md px-1.5 py-0.5 rounded text-[9px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Lượt xem">
              <Eye size={10} /> <span>{data.viewCount || 0}</span>
           </div>
           <div className="bg-black/40 backdrop-blur-md px-1.5 py-0.5 rounded text-[9px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Lượt thích">
              <Heart size={10} /> <span>{data.likeCount || 0}</span>
           </div>
           <div className="bg-black/40 backdrop-blur-md px-1.5 py-0.5 rounded text-[9px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Lượt lưu">
              <Bookmark size={10} /> <span>{data.bookmarkCount || 0}</span>
           </div>
           <div className="bg-black/40 backdrop-blur-md px-1.5 py-0.5 rounded text-[9px] font-semibold text-white flex items-center gap-1 border border-white/10 shadow-sm" title="Bình luận">
              <MessageCircle size={10} /> <span>{data.commentCount || 0}</span>
           </div>
        </div>
      </div>

      {/* 2. Text Info (Below Thumbnail) */}
      <div className="mt-2.5 flex flex-col px-0.5">
        {/* Title */}
        <h3 className={`font-bold text-gray-900 leading-[1.3] line-clamp-2 transition-colors group-hover:text-blue-600 ${
          large ? "text-lg sm:text-xl" : "text-[14px]"
        }`}>
          {data.title}
        </h3>

        {/* Location / Date */}
        <div className="flex items-center gap-1.5 mt-1.5 text-[13px] text-gray-500 font-medium">
          <Clock size={13} className="shrink-0" />
          <span className="truncate">
            {data.readTime ? `${data.readTime} • ` : ""}
            {data.publishedAt
              ? new Date(data.publishedAt).toLocaleDateString("vi-VN")
              : data.date ||
                (data.createdAt
                  ? new Date(data.createdAt).toLocaleDateString("vi-VN")
                  : "")}
          </span>
        </div>

      </div>
    </Motion.div>
  );
});

BlogCard.displayName = "BlogCard";
export default BlogCard;
