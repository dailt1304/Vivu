import React from "react";
import { motion, AnimatePresence } from "framer-motion";
import { MoreHorizontal, FolderHeart } from "lucide-react";

const CollectionCard = ({ data, onClick, onMenuClick, onEdit, onDelete }) => {
  const {
    name,
    locationCount,
    coverImageUrl,
    modifiedDate,
    createdDate,
  } = data;
  const lastUpdated = new Date(modifiedDate || createdDate).toLocaleDateString(
    "vi-VN"
  );

  return (
    <motion.div
      onClick={onClick}
      whileHover={{ y: -6 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: "spring", stiffness: 400, damping: 25 }}
      className="group relative flex flex-col h-full w-full cursor-pointer"
    >
      {/* Cover Image Wrapper - Bento 2.0 aesthetics */}
      <div className="w-full aspect-[4/3] bg-slate-100 relative overflow-hidden rounded-3xl shadow-[0_4px_20px_-10px_rgba(0,0,0,0.08)] mb-4 transition-all duration-700 group-hover:shadow-[0_20px_40px_-15px_rgba(59,130,246,0.2)] ring-1 ring-slate-900/5 group-hover:ring-blue-500/20">
        {coverImageUrl ? (
          <img
            src={coverImageUrl}
            alt={name}
            className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-slate-300 group-hover:text-blue-500 transition-colors duration-500 bg-gradient-to-br from-slate-50 to-slate-200">
            <FolderHeart size={44} strokeWidth={1.5} />
          </div>
        )}
        
        {/* Optical enhancements */}
        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/5 transition-colors duration-500 ease-out" />
        <div className="absolute inset-0 shadow-[inset_0_1px_0_rgba(255,255,255,0.4)] rounded-3xl pointer-events-none" />

        {/* Action Menu Trigger - Glassmorphism */}
        <button
          onClick={(e) => {
            e.stopPropagation();
            onMenuClick && onMenuClick(e);
          }}
          className="absolute top-4 right-4 p-2 rounded-full bg-white/30 backdrop-blur-xl border border-white/40 text-slate-800 hover:bg-white hover:text-blue-600 shadow-sm transition-all z-10 opacity-0 scale-90 group-hover:opacity-100 group-hover:scale-100"
        >
          <MoreHorizontal size={18} strokeWidth={2.5} />
        </button>
      </div>

      {/* Title & Metadata (Outside card) */}
      <div className="flex flex-col grow px-1 text-left">
        <h3 className="font-bold text-slate-900 text-lg md:text-xl tracking-tight leading-snug group-hover:text-blue-600 transition-colors line-clamp-1 mb-1.5">
          {name}
        </h3>
        
        <div className="flex items-center text-slate-500 text-sm font-medium mt-auto gap-3">
          <span className="flex items-center gap-1.5">
            <FolderHeart size={15} strokeWidth={2.5} className="text-blue-500/70" /> 
            {locationCount} địa điểm
          </span>
          <span className="w-1 h-1 rounded-full bg-slate-300" />
          <span className="text-slate-400 font-semibold">{lastUpdated}</span>
        </div>
      </div>

      {/* Floating Dropdown Menu */}
      <AnimatePresence>
        {data.isMenuOpen && (
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: -5 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: -5 }}
            transition={{ type: "spring", stiffness: 400, damping: 25 }}
            className="absolute right-4 top-16 mt-1 w-48 bg-white/90 backdrop-blur-2xl rounded-2xl shadow-[0_8px_30px_rgb(0,0,0,0.12)] border border-white/40 overflow-hidden z-20 py-2"
          >
            <button
              onClick={(e) => {
                e.stopPropagation();
                onEdit && onEdit();
              }}
              className="w-full text-left px-5 py-2.5 text-sm font-bold text-slate-700 hover:bg-slate-50 hover:text-blue-600 transition-colors"
            >
              Chỉnh sửa
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onDelete && onDelete();
              }}
              className="w-full text-left px-5 py-2.5 text-sm font-bold text-red-500 hover:bg-red-50 hover:text-red-600 transition-colors"
            >
              Xóa bộ sưu tập
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};

export default React.memo(CollectionCard);
