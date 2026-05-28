import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { cn } from "@/lib/utils";
import { Search, LogOut, Command, ChevronRight, Home, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import NotificationBell from "../../notifications/NotificationBell";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { useAuth } from "../../../contexts/auth-context";
import { useUser } from "../../../hooks/useUsers";

const BREADCRUMBS_MAP = {
  "/cms": "Trang chủ",
  "/cms/dashboard": "Dashboard",
  "/cms/blogs": "Chia sẻ & Trải nghiệm",
  "/cms/locations": "Quản lý Địa điểm",
  "/cms/support": "Hỗ trợ Khách hàng",
  "/cms/users": "Quản lý Người dùng",
  "/cms/cities": "Quản lý Khu vực",
  "/cms/subscriptions": "Gói Đăng ký & Doanh thu",
};

const CMSHeader = ({ sidebarCollapsed }) => {
  const navigate = useNavigate();
  const location = useLocation();

  const { userId, user: authUser, logout } = useAuth();
  const { data: fullUser } = useUser(userId);
  const user = fullUser || authUser;

  const handleLogout = () => {
    logout();
  };

  const getCurrentBreadcrumb = () => {
    return BREADCRUMBS_MAP[location.pathname] || "Chi tiết";
  };

  return (
    <>
      <header
        className={cn(
          "fixed top-0 right-0 z-30 flex h-16 items-center justify-between border-b border-zinc-200 bg-white/80 backdrop-blur-md px-6 transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)]",
          sidebarCollapsed ? "left-16" : "left-[260px]",
        )}
      >
        {/* Left container: Breadcrumb */}
        <div className="flex items-center gap-2">
           <div className="hidden sm:flex items-center gap-2 text-sm font-medium text-zinc-500">
             <div className="flex items-center gap-1.5 hover:text-zinc-900 transition-colors cursor-pointer" onClick={() => navigate("/cms/dashboard")}>
                <Home className="h-4 w-4" />
             </div>
             <ChevronRight className="h-4 w-4 text-zinc-300" />
             <span className="text-zinc-900 font-semibold">{getCurrentBreadcrumb()}</span>
           </div>
        </div>

        {/* Right side actions */}
        <div className="flex items-center gap-4">

          {/* Notifications with Pulse */}
          <div className="relative">
             <NotificationBell />
             <span className="absolute top-1.5 right-1.5 flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500 border border-white"></span>
             </span>
          </div>

          <div className="w-px h-5 bg-zinc-200 mx-1"></div>

          {/* Quick Logout Button */}
          <button 
            onClick={handleLogout}
            className="flex items-center gap-2 px-3 py-1.5 text-sm font-semibold text-red-600 bg-red-50 hover:bg-red-100 rounded-lg transition-colors focus:ring-2 focus:ring-red-500 focus:ring-offset-1 focus:outline-none"
            title="Đăng xuất"
          >
            <LogOut size={16} />
            <span className="hidden sm:inline">Đăng xuất</span>
          </button>

 
        </div>
      </header>
    </>
  );
};

export default CMSHeader;
