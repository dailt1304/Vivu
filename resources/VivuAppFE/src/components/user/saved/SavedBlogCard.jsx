import React from 'react';
import { motion } from 'framer-motion';
import { Bookmark, Eye, Heart, Image as ImageIcon } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

const SavedBlogCard = ({ blog, onUnbookmark }) => {
  const navigate = useNavigate();

  const handleCardClick = () => {
    navigate(`/inspiration/${blog.slug}`);
  };

  const handleUnbookmarkClick = (e) => {
    e.stopPropagation();
    if (onUnbookmark) {
      onUnbookmark(blog.id);
    }
  };

  return (
    <motion.div
      onClick={handleCardClick}
      whileHover={{ y: -6 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: "spring", stiffness: 400, damping: 25 }}
      className="group relative flex flex-col h-full w-full cursor-pointer"
    >
      {/* Cover Image Wrapper - Bento 2.0 aesthetics */}
      <div className="w-full aspect-[4/3] bg-slate-100 relative overflow-hidden rounded-3xl shadow-[0_4px_20px_-10px_rgba(0,0,0,0.08)] mb-4 transition-all duration-700 group-hover:shadow-[0_20px_40px_-15px_rgba(59,130,246,0.2)] ring-1 ring-slate-900/5 group-hover:ring-blue-500/20">
        {blog.coverImageUrl ? (
          <img
            src={blog.coverImageUrl}
            alt={blog.title}
            className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center bg-slate-100 text-slate-300">
             <ImageIcon size={44} strokeWidth={1.5} />
          </div>
        )}
        
        {/* Optical enhancements */}
        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/5 transition-colors duration-500 ease-out" />
        <div className="absolute inset-0 shadow-[inset_0_1px_0_rgba(255,255,255,0.4)] rounded-3xl pointer-events-none" />

        <button
          onClick={handleUnbookmarkClick}
          className="absolute top-4 right-4 p-2 rounded-full bg-white/30 backdrop-blur-xl border border-white/40 text-slate-800 hover:bg-white hover:text-blue-600 shadow-sm transition-all z-10 opacity-0 scale-90 group-hover:opacity-100 group-hover:scale-100"
          title="Bỏ lưu bài viết"
        >
          <Bookmark size={18} fill="currentColor" strokeWidth={2} />
        </button>
      </div>

      {/* Title & Metadata (Outside card) */}
      <div className="flex flex-col grow px-1">
        <h3 className="font-bold text-slate-900 text-lg md:text-xl tracking-tight leading-snug group-hover:text-blue-600 transition-colors line-clamp-2 mb-3">
          {blog.title}
        </h3>
        
        <div className="mt-auto flex items-center justify-between text-slate-500">
          <div className="flex items-center gap-2">
            {blog.authorAvatarUrl ? (
              <img 
                src={blog.authorAvatarUrl} 
                alt={blog.authorName} 
                className="w-7 h-7 rounded-full object-cover ring-2 ring-white shadow-sm"
              />
            ) : (
               <div className="w-7 h-7 rounded-full bg-blue-50 flex items-center justify-center text-blue-600 text-[10px] font-bold ring-2 ring-white shadow-sm">
                 {blog.authorName?.charAt(0) || 'U'}
               </div>
            )}
            <span className="text-sm font-semibold text-slate-700 truncate max-w-[120px]">{blog.authorName}</span>
          </div>

          <div className="flex items-center gap-3 text-sm font-semibold">
             <span className="flex items-center gap-1.5"><Eye size={14} className="text-slate-400" strokeWidth={2.5}/> {blog.viewCount || 0}</span>
             <span className="flex items-center gap-1.5"><Heart size={14} className="text-slate-400" strokeWidth={2.5}/> {blog.likeCount || 0}</span>
          </div>
        </div>
      </div>
    </motion.div>
  );
};

export default React.memo(SavedBlogCard);
