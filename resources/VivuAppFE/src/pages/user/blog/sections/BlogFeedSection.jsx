import React, { useState } from "react";
import BlogCard from "../../../../components/common/cards/BlogCard";
import { FileText } from "lucide-react";
import { usePublicBlogs } from "../../../../hooks/blogs/useBlogs";
import PropTypes from "prop-types";
import { motion } from "framer-motion";

const BlogSkeletonGrid = () => (
  <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-6 mb-8 pt-2">
    {[...Array(10)].map((_, i) => (
      <div key={`skel-blog-${i}`} className="flex flex-col gap-2.5 w-full">
        <div className="rounded-2xl bg-gray-200 animate-pulse aspect-[4/5] w-full" />
        <div className="h-4 bg-gray-200 rounded-md w-3/4 animate-pulse mt-1" />
        <div className="h-3 bg-gray-200 rounded-md w-1/2 animate-pulse" />
      </div>
    ))}
  </div>
);

const BlogFeedSection = ({
  searchQuery = "",
  bookmarkedBlogIds = new Set(),
  searchData = null,
  searchLoading = false,
  currentPage: externalPage,
  onPageChange,
}) => {
  const [internalPage, setInternalPage] = useState(1);
  const itemsPerPage = 15;

  const isSearching = searchQuery?.length >= 2;
  const currentPage = externalPage ?? internalPage;
  const setCurrentPage = onPageChange ?? setInternalPage;

  // Fetch blogs from API (only when NOT searching)
  const { data: blogsData, isLoading } = usePublicBlogs(
    !isSearching
      ? {
          sortBy: "newest",
          pageSize: itemsPerPage,
          pageNumber: currentPage,
        }
      : null,
  );

  // Use search results when searching, otherwise use regular feed
  const activeData = isSearching ? searchData : blogsData;
  const activeLoading = isSearching ? searchLoading : isLoading;

  const currentBlogs = (activeData?.items || []).filter(
    (b) => b.status?.toLowerCase() !== "deleted",
  );
  const totalPages = activeData?.totalPages || 0;

  return (
    <div className="mt-4">
      {/* Header Row: Title */}
      <h2 className="text-[22px] sm:text-2xl font-bold text-gray-900 mb-6 tracking-tight">
        {isSearching ? "Kết quả tìm kiếm" : "Bài viết chọn lọc"}
      </h2>

      {/* 5 Column Grid */}
      <div
        className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3 sm:gap-6 mb-8 pt-2"
        style={{ contentVisibility: "auto" }}
      >
        {currentBlogs.map((blog, index) => (
          <motion.div
            key={blog.id}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: (index % 6) * 0.05 }}
          >
            <BlogCard
              data={{
                ...blog,
                isBookmarked: bookmarkedBlogIds.has(blog.id?.toLowerCase()),
              }}
            />
          </motion.div>
        ))}
      </div>

      {activeLoading && currentBlogs.length === 0 ? (
        <BlogSkeletonGrid />
      ) : activeLoading && currentBlogs.length > 0 ? (
        <div className="flex justify-center my-6">
          <div className="w-8 h-8 rounded-full border-4 border-blue-200 border-t-blue-600 animate-spin" />
        </div>
      ) : !activeLoading && currentBlogs.length === 0 ? (
        <div className="py-20 text-center text-gray-500 font-medium">
          {isSearching
            ? "Không tìm thấy bài viết nào phù hợp"
            : "Không tìm thấy bài viết nào"}
        </div>
      ) : null}

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex justify-center items-center gap-2 mt-12 mb-8">
          <button
            onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
            disabled={currentPage === 1}
            className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors"
          >
            &lt;
          </button>

          {[...Array(totalPages)].map((_, i) => {
            const pageNum = i + 1;
            if (
              pageNum === 1 ||
              pageNum === totalPages ||
              (pageNum >= currentPage - 1 && pageNum <= currentPage + 1)
            ) {
              return (
                <button
                  key={pageNum}
                  onClick={() => setCurrentPage(pageNum)}
                  className={`w-10 h-10 flex items-center justify-center rounded-xl text-sm font-bold transition-all ${
                    currentPage === pageNum
                      ? "bg-gradient-primary text-white shadow-md shadow-blue-500/20"
                      : "border border-gray-200 text-gray-600 hover:bg-gray-50 hover:text-blue-600"
                  }`}
                >
                  {pageNum}
                </button>
              );
            } else if (
              pageNum === currentPage - 2 ||
              pageNum === currentPage + 2
            ) {
              return (
                <span key={pageNum} className="text-gray-400">
                  ...
                </span>
              );
            }
            return null;
          })}

          <button
            onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
            disabled={currentPage === totalPages}
            className="w-10 h-10 flex items-center justify-center rounded-xl border border-gray-200 text-gray-500 hover:bg-gray-50 hover:text-blue-600 disabled:opacity-50 transition-colors"
          >
            &gt;
          </button>
        </div>
      )}
    </div>
  );
};

BlogFeedSection.propTypes = {
  searchQuery: PropTypes.string,
  onSearchChange: PropTypes.func,
  bookmarkedBlogIds: PropTypes.instanceOf(Set),
  searchData: PropTypes.object,
  searchLoading: PropTypes.bool,
  currentPage: PropTypes.number,
  onPageChange: PropTypes.func,
};

export default BlogFeedSection;
