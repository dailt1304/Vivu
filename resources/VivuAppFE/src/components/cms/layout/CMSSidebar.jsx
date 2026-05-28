import React from "react";
import { NavLink } from "react-router-dom";
import { cn } from "@/lib/utils";
import {
  LayoutDashboard,
  FileText,
  MapPin,
  HeadphonesIcon,
  Users,
  CreditCard,
  ChevronLeft,
  ChevronRight,
  Map,
  ShieldAlert,
  UserCheck,
  Layers
} from "lucide-react";
import { Button } from "@/components/ui/button";


const CMSSidebar = ({ userRoles = [], collapsed, onToggle }) => {
  const normalizedRoles = userRoles.map((r) => r.toUpperCase());
  const isAdmin = normalizedRoles.includes("ADMIN");

  const moderatorMenuItems = [
    {
      path: "/cms/cities",
      label: "Quản lý Khu vực (Tỉnh/TP)",
      icon: Map,
    },
    {
      path: "/cms/categories",
      label: "Danh mục Địa điểm",
      icon: Layers,
    },
    {
      path: "/cms/locations",
      label: "Quản lý Địa điểm",
      icon: MapPin,
    },
    {
      path: "/cms/blogs",
      label: "Quản lý Bài viết",
      icon: FileText,
    },
  ];

  const adminMenuItems = [
    {
      path: "/cms/dashboard",
      label: "Dashboard",
      icon: LayoutDashboard,
    },
    {
      path: "/cms/users",
      label: "Quản lý Người dùng",
      icon: Users,
    },
    {
      path: "/cms/subscriptions",
      label: "Gói Đăng ký & Doanh thu",
      icon: CreditCard,
    },
  ];

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 z-40 h-screen bg-[#0f1117] text-white/90 transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] shadow-xl flex flex-col",
        collapsed ? "w-16" : "w-[260px]",
      )}
    >
      {/* Logo Area */}
      <div className="flex h-16 shrink-0 items-center justify-between border-b border-white/10 px-4 relative">
        <div className={cn("flex items-center transition-opacity duration-300", collapsed ? "opacity-0 invisible absolute" : "opacity-100")}>
          <img src="/vivu_logo.jpg" alt="Vivu CMS Logo" className="h-8 w-auto object-contain rounded-sm" />
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={onToggle}
          className={cn(
             "h-8 w-8 text-slate-400 hover:text-white hover:bg-white/10 shrink-0 transition-transform", 
             collapsed ? "mx-auto" : ""
          )}
        >
          {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
        </Button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 mt-6 px-3 space-y-1 overflow-y-auto hide-scrollbar pb-24">
        {isAdmin && (
          <div className="mb-2">
            <p className={cn("px-3 py-2 text-[10px] font-bold text-slate-500 tracking-[0.2em] uppercase transition-all", collapsed && "opacity-0 h-0 p-0 overflow-hidden")}>
              Administration
            </p>
            {adminMenuItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  cn(
                    "group relative flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200 text-sm font-medium",
                    isActive
                      ? "bg-emerald-500/10 text-emerald-400 font-semibold"
                      : "text-slate-400 hover:text-white hover:bg-white/5",
                    collapsed && "justify-center px-0"
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && !collapsed && (
                       <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-5 bg-emerald-500 rounded-r-full shadow-[0_0_8px_rgba(16,185,129,0.5)]" />
                    )}
                    <item.icon className={cn("h-5 w-5 shrink-0 transition-colors", isActive ? "text-emerald-400" : "text-slate-400 group-hover:text-slate-200")} />
                    
                    {!collapsed && <span className="truncate">{item.label}</span>}
                    
                    {/* Tooltip for Collapsed State */}
                    {collapsed && (
                      <div className="absolute left-full ml-4 px-3 py-1.5 bg-slate-800 text-white text-xs font-medium rounded-md shadow-xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all whitespace-nowrap z-50 border border-slate-700">
                        {item.label}
                        <div className="absolute top-1/2 -left-1 -translate-y-1/2 w-2 h-2 bg-slate-800 rotate-45 border-l border-b border-slate-700"></div>
                      </div>
                    )}
                  </>
                )}
              </NavLink>
            ))}
          </div>
        )}

        {!isAdmin && (
          <div className="mb-2">
            <p className={cn("px-3 py-2 text-[10px] font-bold text-slate-500 tracking-[0.2em] uppercase transition-all", collapsed && "opacity-0 h-0 p-0 overflow-hidden")}>
              Moderation & Support
            </p>
            {moderatorMenuItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  cn(
                    "group relative flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all duration-200 text-sm font-medium",
                    isActive
                      ? "bg-blue-500/10 text-blue-400 font-semibold"
                      : "text-slate-400 hover:text-white hover:bg-white/5",
                    collapsed && "justify-center px-0"
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && !collapsed && (
                       <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-5 bg-blue-500 rounded-r-full shadow-[0_0_8px_rgba(59,130,246,0.5)]" />
                    )}
                    <item.icon className={cn("h-5 w-5 shrink-0 transition-colors", isActive ? "text-blue-400" : "text-slate-400 group-hover:text-slate-200")} />
                    
                    {!collapsed && <span className="truncate">{item.label}</span>}

                    {collapsed && (
                      <div className="absolute left-full ml-4 px-3 py-1.5 bg-slate-800 text-white text-xs font-medium rounded-md shadow-xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all whitespace-nowrap z-50 border border-slate-700">
                        {item.label}
                        <div className="absolute top-1/2 -left-1 -translate-y-1/2 w-2 h-2 bg-slate-800 rotate-45 border-l border-b border-slate-700"></div>
                      </div>
                    )}
                  </>
                )}
              </NavLink>
            ))}
          </div>
        )}
      </nav>

      {/* Role Badge Profile Section */}
      <div className="absolute bottom-0 left-0 right-0 p-4 bg-gradient-to-t from-[#0f1117] via-[#0f1117] to-transparent pt-8">
        <div className={cn(
           "flex items-center gap-3 p-2 rounded-xl transition-all duration-300 border border-white/5",
           isAdmin ? "bg-emerald-950/30" : "bg-blue-950/30",
           collapsed ? "justify-center px-0" : "px-3"
        )}>
           <div className="relative shrink-0 flex items-center justify-center w-8 h-8 rounded-lg bg-white/10">
              {isAdmin ? <ShieldAlert className="h-4 w-4 text-emerald-400" /> : <UserCheck className="h-4 w-4 text-blue-400" />}
              <span className="absolute -top-1 -right-1 flex h-2.5 w-2.5">
                 <span className={cn("animate-ping absolute inline-flex h-full w-full rounded-full opacity-75", isAdmin ? "bg-emerald-400" : "bg-blue-400")}></span>
                 <span className={cn("relative inline-flex rounded-full h-2.5 w-2.5", isAdmin ? "bg-emerald-500" : "bg-blue-500")}></span>
               </span>
           </div>
           
           {!collapsed && (
              <div className="flex flex-col min-w-0 justify-center">
                 <span className="text-sm font-semibold text-slate-200 truncate">
                   {isAdmin ? "Admin" : "Moderator"}
                 </span>
              </div>
           )}

           {collapsed && (
              <div className="absolute left-full ml-4 px-3 py-2 bg-slate-800 text-white text-xs font-semibold rounded-md shadow-xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all whitespace-nowrap z-50 border border-slate-700">
                {isAdmin ? "Admin" : "Moderator"}
                <div className="absolute top-1/2 -left-1 -translate-y-1/2 w-2 h-2 bg-slate-800 rotate-45 border-l border-b border-slate-700"></div>
              </div>
           )}
        </div>
      </div>
    </aside>
  );
};

export default CMSSidebar;
