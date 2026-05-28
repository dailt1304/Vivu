import React from "react";
import { motion } from "framer-motion";

const EmptyState = ({ icon, title, description, actionLabel, onAction, type = "default" }) => {
  return (
    <motion.div 
      initial={{ opacity: 0, y: 30 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ type: "spring", stiffness: 300, damping: 30 }}
      className="flex flex-col items-center justify-center px-6 md:px-8 py-10 md:py-16 text-center bg-white/40 backdrop-blur-3xl rounded-3xl md:rounded-[2.5rem] shadow-[0_24px_48px_-12px_rgba(0,0,0,0.06)] border border-white/60 max-w-2xl mx-auto my-6 md:my-12 relative overflow-hidden group"
    >
      {/* Dynamic Background Patterns */}
      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        {/* Animated Mesh Gradients */}
        <div className="absolute -top-[20%] -left-[10%] w-[50%] h-[50%] bg-blue-100/30 blur-[80px] rounded-full animate-pulse" />
        <div className="absolute -bottom-[20%] -right-[10%] w-[50%] h-[50%] bg-indigo-100/20 blur-[80px] rounded-full animate-pulse" style={{ animationDelay: '1s' }} />
        
        {/* Type-specific patterns */}
        {type === "trips" && (
          <svg className="absolute inset-0 w-full h-full opacity-[0.04]" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M-10,50 Q25,30 50,50 T110,50" fill="none" stroke="currentColor" strokeWidth="0.5" strokeDasharray="2,2" className="text-blue-500" />
            <path d="M-10,70 Q25,50 50,70 T110,70" fill="none" stroke="currentColor" strokeWidth="0.5" strokeDasharray="2,2" className="text-blue-400" />
            <circle cx="20" cy="45" r="0.8" className="fill-blue-500" />
            <circle cx="80" cy="55" r="0.8" className="fill-blue-500" />
          </svg>
        )}
        {type === "blogs" && (
          <svg className="absolute inset-0 w-full h-full opacity-[0.04]" viewBox="0 0 100 100" preserveAspectRatio="none">
            <line x1="10" y1="20" x2="90" y2="20" stroke="currentColor" strokeWidth="0.5" className="text-slate-400" />
            <line x1="10" y1="35" x2="70" y2="35" stroke="currentColor" strokeWidth="0.5" className="text-slate-400" />
            <line x1="10" y1="50" x2="90" y2="50" stroke="currentColor" strokeWidth="0.5" className="text-slate-400" />
            <line x1="10" y1="65" x2="80" y2="65" stroke="currentColor" strokeWidth="0.5" className="text-slate-400" />
          </svg>
        )}
      </div>
      
      {/* Central Icon Container (Floating Squircle) */}
      <motion.div 
        animate={{ y: [0, -8, 0] }}
        transition={{ repeat: Infinity, duration: 4, ease: "easeInOut" }}
        className="relative z-10 mb-6 md:mb-8 w-16 h-16 md:w-20 md:h-20 rounded-[30%] bg-white shadow-[0_12px_24px_-8px_rgba(0,0,0,0.1)] border border-slate-100 flex items-center justify-center group-hover:scale-105 transition-transform duration-500"
      >
        {/* Glow effect */}
        <div className="absolute inset-0 rounded-[30%] bg-blue-400/10 blur-xl group-hover:bg-blue-400/20 transition-colors" />
        <div className="relative z-20 transform scale-90 md:scale-110">
          {icon}
        </div>
      </motion.div>
      
      <h3 className="text-xl md:text-3xl font-black text-slate-900 tracking-tight mb-2 md:mb-3 relative z-10 leading-tight">
        {title}
      </h3>
      <p className="text-slate-500 font-medium leading-relaxed max-w-sm text-xs md:text-base mb-8 md:mb-10 relative z-10">
        {description}
      </p>
      
      {actionLabel && onAction && (
        <motion.button
          whileHover={{ scale: 1.04, y: -2 }}
          whileTap={{ scale: 0.98 }}
          onClick={onAction}
          className="relative z-10 bg-gradient-primary text-white font-bold h-10 md:h-12 px-6 md:px-8 rounded-xl shadow-[0_12px_24px_-8px_rgba(59,130,246,0.5)] hover:shadow-[0_16px_32px_-8px_rgba(59,130,246,0.6)] transition-all tracking-wide text-xs md:text-sm flex items-center gap-2 group-hover:ring-4 ring-blue-500/5"
        >
          {actionLabel}
        </motion.button>
      )}
    </motion.div>
  );
};

export default React.memo(EmptyState);
