import React from "react";
import { FolderSearch, Plus } from "lucide-react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { useLocation } from "react-router-dom";

const EmptyState = ({
  icon: Icon = FolderSearch,
  title = "Không tìm thấy dữ liệu",
  description = "Hiện tại không có dữ liệu nào khớp với tìm kiếm của bạn hoặc danh sách đang trống.",
  actionLabel,
  onAction,
  className = "",
  inline = false,
}) => {
  const loc = useLocation();
  const isAdminTheme = ['/cms/dashboard', '/cms/users', '/cms/subscriptions'].some(p => loc.pathname.includes(p));
  
  const glowClass = isAdminTheme ? "bg-emerald-100" : "bg-blue-100";
  const iconClass = isAdminTheme ? "text-emerald-500/80" : "text-blue-500/80";
  const hoverClass = isAdminTheme ? "hover:text-emerald-600" : "hover:text-blue-600";

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.4, ease: [0.16, 1, 0.3, 1] }}
      className={`flex flex-col items-center justify-center text-center ${inline ? 'py-12' : 'min-h-[400px] py-16'} ${className}`}
    >
      <div className="relative mb-6">
        <div className={`absolute inset-0 rounded-full blur-xl opacity-50 animate-pulse ${glowClass}`}></div>
        <div className="relative h-20 w-20 rounded-2xl bg-zinc-50 border border-zinc-100 shadow-sm flex items-center justify-center">
          <Icon className={`h-10 w-10 stroke-[1.5] ${iconClass}`} />
        </div>
      </div>
      
      <h3 className="text-lg font-semibold text-zinc-900 mb-2 tracking-tight">
        {title}
      </h3>
      
      <p className="text-sm text-zinc-500 max-w-[320px] leading-relaxed mb-6">
        {description}
      </p>

      {actionLabel && onAction && (
        <Button 
           onClick={onAction} 
           className={`cms-btn-interactive bg-white border border-zinc-200 text-zinc-700 hover:bg-zinc-50 shadow-sm ${hoverClass}`}
        >
          <Plus className="h-4 w-4 mr-2" />
          {actionLabel}
        </Button>
      )}
    </motion.div>
  );
};

export default EmptyState;
