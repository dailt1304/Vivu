import React from "react";
import { useNavigate } from "react-router-dom";
import { motion as Motion } from "framer-motion";
import { Bookmark, Loader2 } from "lucide-react";
import { useMyBookmarks } from "../../../hooks/blogs/useBlogs";
import BlogCard from "../../common/cards/BlogCard";
import { EmptyState } from "../../common";

/**
 * Tab for displaying bookmarked blogs on user's profile
 */
const SavedBlogsTab = () => {
  const navigate = useNavigate();
  const { data, isLoading } = useMyBookmarks(1, 50);
  const blogs = data?.items || [];

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-400">
        <Loader2 size={40} className="animate-spin mb-4 text-blue-500" />
        <p className="font-medium">Đang tải bài viết đã lưu...</p>
      </div>
    );
  }

  if (blogs.length === 0) {
    return (
      <EmptyState
        icon={Bookmark}
        title="Bạn chưa lưu bài viết nào"
        subtitle="Hãy khám phá các bài viết thú vị và nhấn lưu để xem lại sau!"
        actionLabel="Khám phá ngay"
        onAction={() => navigate("/inspiration")}
      />
    );
  }

  return (
    <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
      {/* Header Info */}
      <div className="bg-blue-50/50 rounded-2xl p-6 border border-blue-100 flex items-center justify-between">
        <div>
          <h3 className="text-xl font-bold text-gray-900 mb-1">
            Bài viết đã lưu
          </h3>
          <p className="text-sm text-gray-500">
            Bạn đã lưu {blogs.length} bài viết
          </p>
        </div>
        <div className="p-3 bg-white rounded-xl shadow-sm text-blue-600">
          <Bookmark size={24} fill="currentColor" />
        </div>
      </div>

      {/* Blogs Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {blogs.map((blog, index) => (
          <Motion.div
            key={blog.id}
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: index * 0.05 }}
          >
            <BlogCard data={blog} />
          </Motion.div>
        ))}
      </div>
    </div>
  );
};

export default SavedBlogsTab;
