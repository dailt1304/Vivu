import React from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { PenSquare, Eye, Heart, MessageCircle, Loader2 } from "lucide-react";
import PropTypes from "prop-types";
import { useMyBlogs } from "../../../hooks/blogs/useBlogs";

/**
 * User blogs tab for profile page
 */
const UserBlogsTab = ({ isOwnProfile = true }) => {
  const navigate = useNavigate();
  const { data, isLoading } = useMyBlogs(1, 50);
  const blogs = data?.items || [];

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-400">
        <Loader2 size={40} className="animate-spin mb-4 text-blue-500" />
        <p className="font-medium">Đang tải bài viết...</p>
      </div>
    );
  }

  if (blogs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400">
        <PenSquare size={48} className="mb-4 opacity-50" />
        <p className="font-medium mb-2">Chưa có bài viết nào</p>
        {isOwnProfile && (
          <button
            onClick={() => navigate("/inspiration/create")}
            className="mt-4 px-5 py-2.5 bg-gradient-to-r from-blue-500 to-indigo-600 text-white font-bold rounded-xl hover:shadow-lg transition-all"
          >
            Viết bài đầu tiên
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Stats Summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-gradient-to-br from-purple-50 to-indigo-50 rounded-xl p-4 text-center">
          <div className="text-2xl font-black text-purple-600">
            {blogs.length}
          </div>
          <div className="text-xs text-gray-500">Bài viết</div>
        </div>
        <div className="bg-gradient-to-br from-pink-50 to-rose-50 rounded-xl p-4 text-center">
          <div className="text-2xl font-black text-rose-600">
            {blogs.reduce((sum, b) => sum + (b.likeCount || 0), 0)}
          </div>
          <div className="text-xs text-gray-500">Lượt thích</div>
        </div>
        <div className="bg-gradient-to-br from-blue-50 to-cyan-50 rounded-xl p-4 text-center">
          <div className="text-2xl font-black text-blue-600">
            {blogs
              .reduce((sum, b) => sum + (b.viewCount || 0), 0)
              .toLocaleString()}
          </div>
          <div className="text-xs text-gray-500">Lượt xem</div>
        </div>
      </div>

      {/* Blogs Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {blogs.map((blog, index) => (
          <motion.div
            key={blog.id}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.1 }}
            onClick={() => navigate(`/inspiration/${blog.id}`)}
            className="group bg-white rounded-2xl border border-gray-100 overflow-hidden shadow-sm hover:shadow-lg transition-all cursor-pointer flex flex-col"
          >
            {/* Image */}
            <div className="relative h-48 overflow-hidden shrink-0 bg-gray-100">
              <img
                src={
                  blog.thumbnailUrl ||
                  "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=800"
                }
                alt={blog.title}
                className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />

              {/* Status/Category Badge */}
              <span
                className={`absolute top-3 left-3 px-2.5 py-1 backdrop-blur-sm text-xs font-bold rounded-full ${
                  blog.status === "Published"
                    ? "bg-white/90 text-gray-700"
                    : "bg-amber-100/90 text-amber-800"
                }`}
              >
                {blog.status === "Published" ? "Đã đăng" : "Bản nháp"}
              </span>

              {/* Stats Overlay */}
              <div className="absolute bottom-3 left-3 right-3 flex items-center justify-between text-white text-xs">
                <div className="flex items-center gap-4">
                  <span className="flex items-center gap-1">
                    <Eye size={14} />
                    {blog.viewCount?.toLocaleString() || 0}
                  </span>
                  <span className="flex items-center gap-1">
                    <Heart size={14} />
                    {blog.likeCount || 0}
                  </span>
                  <span className="flex items-center gap-1">
                    <MessageCircle size={14} />
                    {blog.commentCount || 0}
                  </span>
                </div>
              </div>
            </div>

            {/* Content */}
            <div className="p-4 flex-1 flex flex-col">
              <h3 className="font-bold text-gray-900 line-clamp-2 group-hover:text-blue-600 transition-colors">
                {blog.title || "Bài viết không tiêu đề"}
              </h3>
              <div className="mt-auto pt-2">
                <p className="text-xs text-gray-500">
                  {blog.publishedAt
                    ? new Date(blog.publishedAt).toLocaleDateString("vi-VN")
                    : new Date(blog.createdAt).toLocaleDateString("vi-VN")}
                </p>
              </div>
            </div>
          </motion.div>
        ))}
      </div>

      {/* Write New Blog CTA */}
      {isOwnProfile && (
        <button
          onClick={() => navigate("/inspiration/create")}
          className="w-full py-4 border-2 border-dashed border-gray-200 rounded-2xl text-gray-500 font-medium hover:border-blue-400 hover:text-blue-600 hover:bg-blue-50/50 transition-all flex items-center justify-center gap-2"
        >
          <PenSquare size={20} />
          Viết bài mới
        </button>
      )}
    </div>
  );
};

UserBlogsTab.propTypes = {
  isOwnProfile: PropTypes.bool,
};

export default UserBlogsTab;
