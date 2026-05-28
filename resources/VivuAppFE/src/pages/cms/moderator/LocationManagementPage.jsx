import React, { useState } from "react";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { MapPin, FileWarning, Clock } from "lucide-react";
import LocationReportsTab from "@/components/cms/locations/LocationReportsTab";
import LocationListTab from "@/components/cms/locations/LocationListTab";
import PendingLocationsTab from "@/components/cms/locations/PendingLocationsTab";
import { useAuth } from "@/contexts/auth-context";
import { useLocation } from "react-router-dom";

const LocationManagementPage = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState("list");

  const userRoles = Array.isArray(user?.roles) ? user.roles : [];
  const isAdmin = userRoles.map((r) => r.toUpperCase()).includes("ADMIN");

  const canSeeReports = true;

  const loc = useLocation();
  const isAdminTheme = [
    "/cms/dashboard",
    "/cms/users",
    "/cms/subscriptions",
  ].some((p) => loc.pathname.includes(p));
  const tabBorderClass = isAdminTheme
    ? "data-[state=active]:border-emerald-500"
    : "data-[state=active]:border-blue-500";

  return (
    <div className="cms-animate-page space-y-6">
      <div className="cms-animate-stagger space-y-6">
        <div className="border-b border-zinc-200 pb-6">
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">
            Quản lý Địa điểm
          </h1>
          <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
            Xem danh sách, kiểm duyệt đề xuất địa điểm mới và xử lý báo cáo vi
            phạm
          </p>
        </div>

        <div>
          <Tabs
            value={activeTab}
            onValueChange={setActiveTab}
            className="w-full"
          >
            <TabsList className="bg-transparent p-0 h-auto gap-6 pb-2 border-b border-zinc-200 w-full justify-start rounded-none mb-6">
              <TabsTrigger
                value="list"
                className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium flex items-center gap-2 ${tabBorderClass}`}
              >
                <MapPin className="h-4 w-4" />
                Danh sách địa điểm
              </TabsTrigger>
              <TabsTrigger
                value="pending"
                className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium flex items-center gap-2 ${tabBorderClass}`}
              >
                <Clock className="h-4 w-4" />
                <span className="flex items-center gap-2">Chờ xét duyệt</span>
              </TabsTrigger>
              {canSeeReports && (
                <TabsTrigger
                  value="reports"
                  className={`data-[state=active]:bg-transparent data-[state=active]:shadow-none data-[state=active]:border-b-2 rounded-none pb-2 pt-0 px-1 text-zinc-500 data-[state=active]:text-zinc-900 font-medium flex items-center gap-2 ${tabBorderClass}`}
                >
                  <FileWarning className="h-4 w-4" />
                  Báo cáo lỗi
                </TabsTrigger>
              )}
            </TabsList>

            <div className="bg-white rounded-xl shadow-[var(--shadow-cms-card)] border border-zinc-100/80 overflow-hidden">
              <TabsContent
                value="list"
                className="border-none p-0 mt-0 focus-visible:outline-none"
              >
                <div className="p-6">
                  <LocationListTab />
                </div>
              </TabsContent>

              <TabsContent
                value="pending"
                className="border-none p-0 mt-0 focus-visible:outline-none"
              >
                <div className="p-6">
                  <PendingLocationsTab />
                </div>
              </TabsContent>

              {canSeeReports && (
                <TabsContent
                  value="reports"
                  className="border-none p-0 mt-0 focus-visible:outline-none"
                >
                  <div className="p-6">
                    <LocationReportsTab />
                  </div>
                </TabsContent>
              )}
            </div>
          </Tabs>
        </div>
      </div>
    </div>
  );
};

export default LocationManagementPage;
