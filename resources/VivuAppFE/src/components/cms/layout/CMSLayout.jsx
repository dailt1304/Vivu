import React, { useState } from "react";
import { Outlet } from "react-router-dom";
import { cn } from "@/lib/utils";
import CMSSidebar from "./CMSSidebar";
import CMSHeader from "./CMSHeader";

const CMSLayout = ({ userRoles }) => {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className="min-h-screen bg-[var(--color-cms-bg)]">
      {/* Sidebar */}
      <CMSSidebar
        userRoles={userRoles}
        collapsed={sidebarCollapsed}
        onToggle={() => setSidebarCollapsed(!sidebarCollapsed)}
      />

      {/* Header */}
      <CMSHeader userRoles={userRoles} sidebarCollapsed={sidebarCollapsed} />

      {/* Main Content */}
      <main
        className={cn(
          "pt-16 min-h-screen transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] font-cms",
          sidebarCollapsed ? "ml-16" : "ml-[260px]",
        )}
      >
        <div className="p-6 lg:p-8 max-w-[1440px] mx-auto">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default CMSLayout;
