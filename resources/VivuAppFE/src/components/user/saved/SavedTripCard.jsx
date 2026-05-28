import React from 'react';
import { motion } from 'framer-motion';
import { Heart, Copy, MapPin, Calendar, Users, Image as ImageIcon } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

const SavedTripCard = ({ trip, onUnfavorite, onCopy }) => {
  const navigate = useNavigate();

  const handleCardClick = () => {
    navigate(`/trips/public/${trip.id}`);
  };

  const handleUnfavoriteClick = (e) => {
    e.stopPropagation();
    if (onUnfavorite) onUnfavorite(trip.id);
  };

  const handleCopyClick = (e) => {
    e.stopPropagation();
    if (onCopy) onCopy(trip.id);
  };

  return (
    <motion.div
      onClick={handleCardClick}
      whileHover={{ y: -6 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: "spring", stiffness: 400, damping: 25 }}
      className="group relative flex flex-col h-full w-full cursor-pointer"
    >
      <div className="w-full aspect-[4/3] bg-slate-100 relative overflow-hidden rounded-3xl shadow-[0_4px_20px_-10px_rgba(0,0,0,0.08)] mb-4 transition-all duration-700 group-hover:shadow-[0_20px_40px_-15px_rgba(59,130,246,0.2)] ring-1 ring-slate-900/5 group-hover:ring-blue-500/20">
        {(trip.coverImageUrl || trip.coverUrl) ? (
          <img
            src={trip.coverImageUrl || trip.coverUrl}
            alt={trip.title}
            className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-105"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center bg-slate-100 text-slate-300">
             <ImageIcon size={44} strokeWidth={1.5} />
          </div>
        )}
        
        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/5 transition-colors duration-500 ease-out" />
        <div className="absolute inset-0 shadow-[inset_0_1px_0_rgba(255,255,255,0.4)] rounded-3xl pointer-events-none" />

        {/* Member Count Badge (Glassmorphism top-left) */}
        <div className="absolute top-4 left-4 flex items-center gap-1.5 text-xs font-bold text-slate-800 bg-white/70 backdrop-blur-md px-3 py-1.5 rounded-full ring-1 ring-white/50 shadow-sm z-10 transition-opacity duration-300">
           <Users size={12} strokeWidth={2.5} />
           <span>{trip.memberCount || 1}</span>
        </div>

        <button
          onClick={handleUnfavoriteClick}
          className="absolute top-4 right-4 p-2 rounded-full bg-white/30 backdrop-blur-xl border border-white/40 text-slate-800 hover:bg-white hover:text-red-500 shadow-sm transition-all z-10 opacity-0 scale-90 group-hover:opacity-100 group-hover:scale-100"
          title="Bỏ thích"
        >
          <Heart size={18} fill="currentColor" strokeWidth={2} />
        </button>
      </div>

      <div className="flex flex-col grow px-1">
        <h3 className="font-bold text-slate-900 text-lg md:text-xl tracking-tight leading-snug line-clamp-2 group-hover:text-blue-600 transition-colors mb-2">
          {trip.title}
        </h3>

        <div className="flex flex-wrap items-center gap-3 text-sm font-semibold text-slate-500 mt-1 mb-4">
          <span className="flex items-center gap-1 bg-blue-50 text-blue-600 px-2.5 py-1 rounded-md">
            <MapPin size={13} className="text-blue-500" strokeWidth={2.5}/> {trip.cityName || 'N/A'}
          </span>
          <span className="flex items-center gap-1">
            <Calendar size={13} strokeWidth={2.5}/> {trip.startDate ? new Date(trip.startDate).toLocaleDateString('vi-VN') : 'N/A'}
          </span>
        </div>

        <div className="mt-auto flex items-center justify-between">
          <div className="flex items-center gap-2">
            {(trip.ownerAvatarUrl || trip.ownerAvatar) ? (
              <img 
                src={trip.ownerAvatarUrl || trip.ownerAvatar} 
                alt={trip.ownerName} 
                className="w-7 h-7 rounded-full object-cover ring-2 ring-white shadow-sm"
              />
            ) : (
               <div className="w-7 h-7 rounded-full bg-blue-50 flex items-center justify-center text-blue-600 text-[10px] font-bold ring-2 ring-white shadow-sm">
                 {trip.ownerName?.charAt(0) || 'U'}
               </div>
            )}
            <span className="text-sm font-semibold text-slate-700 truncate max-w-[100px]">{trip.ownerName}</span>
          </div>
          
          <button
              onClick={handleCopyClick}
              className="px-3 py-1.5 rounded-full bg-slate-100 text-slate-600 text-xs font-bold hover:bg-slate-200 transition-colors ring-1 ring-slate-200/50 flex items-center gap-1.5 group-hover:bg-blue-600 group-hover:text-white group-hover:ring-blue-600/50"
              title="Lưu bản sao chuyến đi"
            >
              <Copy size={13} strokeWidth={2.5} /> Sao chép
            </button>
        </div>
      </div>
    </motion.div>
  );
};

export default React.memo(SavedTripCard);
