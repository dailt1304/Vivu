import React, { useMemo, Suspense, lazy } from "react";
import {
  MapPin,
  Clock,
  AlertTriangle,
  Users,
  CreditCard,
  DollarSign,
  ArrowRight,
  RefreshCw,
  FileText,
  Map,
  TrendingUp,
  Bell,
  BarChart3,
} from "lucide-react";
import { Link } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useDashboardData } from "@/hooks/dashboard/useDashboard";
import { useActivePlans } from "@/hooks/subscriptions/useSubscriptions";

// Dynamic imports for Charts
const LocationCategoryChart = lazy(
  () => import("@/components/cms/dashboard/LocationCategoryChart"),
);
const LocationReportsChart = lazy(
  () => import("@/components/cms/dashboard/LocationReportsChart"),
);

const formatCurrency = (val) => {
  if (!val && val !== 0) return "—";
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(val);
};

const formatNumber = (val) => {
  if (!val && val !== 0) return "—";
  return val.toLocaleString("vi-VN");
};

const KpiCard = ({ icon: Icon, label, value, color = "emerald", loading }) => (
  <div className={`border-l-2 border-${color}-400 pl-4 py-1`}>
    <p className="text-xs font-medium tracking-wider uppercase text-zinc-400 flex items-center gap-1.5">
      <Icon className="h-3.5 w-3.5" /> {label}
    </p>
    <p className="text-2xl font-semibold text-zinc-900 mt-2 font-[family-name:var(--font-cms-mono)] tabular-nums tracking-tight">
      {loading ? <Skeleton className="h-8 w-24" /> : value}
    </p>
  </div>
);

const DashboardPage = () => {
  const { dashboardData, isLoading, refresh } = useDashboardData();
  const { plans } = useActivePlans();
  const totalPackages = plans?.length || 0;

  const d = dashboardData || {};
  const users = d.users || {};
  const revenue = d.revenue || {};
  const content = d.content || {};
  const locations = d.locations || {};

  const greeting = useMemo(() => {
    const hour = new Date().getHours();
    return hour < 12
      ? "Chào buổi sáng"
      : hour < 18
        ? "Chào buổi chiều"
        : "Chào buổi tối";
  }, []);

  // Revenue trend
  const revenueTrend = useMemo(() => {
    if (!revenue.revenuePreviousMonth || revenue.revenuePreviousMonth === 0)
      return null;
    const diff =
      ((revenue.revenueMTD - revenue.revenuePreviousMonth) /
        revenue.revenuePreviousMonth) *
      100;
    return { value: Math.abs(diff).toFixed(1), up: diff >= 0 };
  }, [revenue]);

  return (
    <div className="cms-animate-page">
      <div className="cms-animate-stagger space-y-8">
        {/* Section 1: Header */}
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 border-b border-zinc-200 pb-6">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-zinc-900">
              {greeting}, Admin
            </h1>
            <p className="text-sm text-zinc-500 mt-1.5 leading-relaxed">
              {isLoading
                ? "Đang tải dữ liệu..."
                : `${formatNumber(users.totalUsers)} người dùng · Doanh thu tháng ${formatCurrency(revenue.revenueMTD)}`}
            </p>
          </div>
          <Button
            onClick={refresh}
            variant="outline"
            size="sm"
            disabled={isLoading}
            className="cms-btn-interactive text-zinc-600 bg-white"
          >
            <RefreshCw
              className={
                isLoading
                  ? "h-4 w-4 mr-2 animate-spin"
                  : "h-4 w-4 mr-2"
              }
            />
            Cập nhật
          </Button>
        </div>

        {/* Section 2: KPI Metrics — Row 1 (Primary) */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
          <KpiCard
            icon={Users}
            label="Tổng người dùng"
            value={formatNumber(users.totalUsers)}
            loading={isLoading}
          />
          <KpiCard
            icon={CreditCard}
            label="Lượt đăng ký (Active)"
            value={formatNumber(users.activeSubscriptions)}
            loading={isLoading}
          />
          <KpiCard
            icon={DollarSign}
            label="Doanh thu (Tháng)"
            value={
              <span className="flex items-center gap-2">
                {formatCurrency(revenue.revenueMTD)}
                {revenueTrend && (
                  <span
                    className={`text-xs font-medium px-1.5 py-0.5 rounded ${
                      revenueTrend.up
                        ? "bg-emerald-50 text-emerald-600"
                        : "bg-red-50 text-red-600"
                    }`}
                  >
                    {revenueTrend.up ? "↑" : "↓"} {revenueTrend.value}%
                  </span>
                )}
              </span>
            }
            loading={isLoading}
          />
          <KpiCard
            icon={MapPin}
            label="Địa điểm cần duyệt"
            value={formatNumber(locations.pendingSubmissionsCount)}
            loading={isLoading}
            color="amber"
          />
        </div>

        {/* Section 2b: KPI Metrics — Row 2 (Secondary/Action) */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
          <KpiCard
            icon={TrendingUp}
            label="Users mới (Tháng)"
            value={formatNumber(users.newUsersThisMonth)}
            loading={isLoading}
            color="blue"
          />
          <KpiCard
            icon={FileText}
            label="Bài viết chờ duyệt"
            value={formatNumber(content.pendingBlogs)}
            loading={isLoading}
            color="amber"
          />
          <KpiCard
            icon={AlertTriangle}
            label="Báo cáo blog"
            value={formatNumber(content.pendingBlogReports)}
            loading={isLoading}
            color="red"
          />
          <KpiCard
            icon={Bell}
            label="Lượt đăng ký sắp hết hạn"
            value={formatNumber(users.expiringSubscriptions)}
            loading={isLoading}
            color="amber"
          />
        </div>

        {/* Section 3: Charts Row */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <Card className="lg:col-span-2 shadow-[var(--shadow-cms-card)] border-zinc-100 rounded-xl overflow-hidden">
            <CardHeader className="border-b border-zinc-100/50 bg-zinc-50/50">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-lg font-medium tracking-tight text-zinc-800">
                    Phân bố danh mục
                  </CardTitle>
                </div>
                <div className="h-8 w-8 rounded-full bg-emerald-50 flex items-center justify-center">
                  <MapPin className="h-4 w-4 text-emerald-600" />
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6">
              <div className="h-[300px]">
                {isLoading ? (
                  <Skeleton className="h-full w-full" />
                ) : (
                  <Suspense
                    fallback={<Skeleton className="h-full w-full" />}
                  >
                    <LocationCategoryChart
                      data={locations.locationsByCategory || []}
                    />
                  </Suspense>
                )}
              </div>
            </CardContent>
          </Card>

          <Card className="shadow-[var(--shadow-cms-card)] border-zinc-100 rounded-xl overflow-hidden">
            <CardHeader className="border-b border-zinc-100/50 bg-zinc-50/50">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-lg font-medium tracking-tight text-zinc-800">
                    Báo cáo Mới
                  </CardTitle>
                </div>
                <div className="h-8 w-8 rounded-full bg-amber-50 flex items-center justify-center">
                  <AlertTriangle className="h-4 w-4 text-amber-600" />
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-6">
              <div className="h-[220px]">
                {isLoading ? (
                  <Skeleton className="h-full w-full rounded-full" />
                ) : (
                  <Suspense
                    fallback={
                      <Skeleton className="h-full w-full rounded-full" />
                    }
                  >
                    <LocationReportsChart
                      data={locations.reportsByType || {}}
                    />
                  </Suspense>
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Section 4 & 5: System Overview & Quick Links */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* System Overview — replaces "Recent Activity" mock */}
          <Card className="shadow-[var(--shadow-cms-card)] border-zinc-100 rounded-xl">
            <CardHeader>
              <CardTitle className="text-lg font-medium tracking-tight text-zinc-800 flex items-center gap-2">
                <BarChart3 className="h-5 w-5 text-emerald-500" />
                Tổng quan hệ thống
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <Link to="/cms/users" className="group block">
                  <div className="flex items-center justify-between p-3 rounded-lg hover:bg-zinc-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-lg bg-blue-50 flex items-center justify-center text-blue-600">
                        <Users className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="text-sm font-medium text-zinc-900">
                          Người dùng
                        </p>
                        <p className="text-xs text-zinc-500">
                          {isLoading
                            ? "..."
                            : `${formatNumber(users.totalUsers)} tổng · ${formatNumber(users.newUsersThisWeek)} mới tuần này`}
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-4 h-4 text-zinc-300 group-hover:text-zinc-500 transition-colors" />
                  </div>
                </Link>

                <Link to="/cms/subscriptions" className="group block">
                  <div className="flex items-center justify-between p-3 rounded-lg hover:bg-zinc-50 transition-colors">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-lg bg-emerald-50 flex items-center justify-center text-emerald-600">
                        <CreditCard className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="text-sm font-medium text-zinc-900">
                          Gói dịch vụ & Doanh thu
                        </p>
                        <p className="text-xs text-zinc-500">
                          {isLoading
                            ? "..."
                            : `${totalPackages} gói cước · ${formatNumber(users.activeSubscriptions)} lượt đăng ký`}
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-4 h-4 text-zinc-300 group-hover:text-zinc-500 transition-colors" />
                  </div>
                </Link>


                <div className="flex items-center justify-between p-3 rounded-lg">
                  <div className="flex items-center gap-3">
                    <div className="w-9 h-9 rounded-lg bg-teal-50 flex items-center justify-center text-teal-600">
                      <Map className="w-4 h-4" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-zinc-900">
                        Lịch trình du lịch
                      </p>
                      <p className="text-xs text-zinc-500">
                        {isLoading
                          ? "..."
                          : `${formatNumber(content.totalTrips)} lịch trình đã tạo`}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <div className="space-y-4">
            <h2 className="text-lg font-medium tracking-tight text-zinc-800 mb-4 px-1">
              Lối tắt
            </h2>

            <Link to="/cms/users" className="group block">
              <Card className="shadow-sm border-zinc-200 hover:border-emerald-300 hover:shadow-md transition-all duration-200">
                <div className="p-4 flex items-center gap-4">
                  <div className="w-10 h-10 rounded-lg bg-blue-50 flex items-center justify-center text-blue-600">
                    <Users className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-zinc-900 group-hover:text-emerald-600 transition-colors">
                      Quản lý người dùng
                    </h3>
                    <p className="text-xs text-zinc-500 mt-0.5">
                      Tìm kiếm, khóa tài khoản, xem lịch sử
                    </p>
                  </div>
                </div>
              </Card>
            </Link>


            <Link to="/cms/subscriptions" className="group block">
              <Card className="shadow-sm border-zinc-200 hover:border-emerald-300 hover:shadow-md transition-all duration-200">
                <div className="p-4 flex items-center gap-4">
                  <div className="w-10 h-10 rounded-lg bg-emerald-50 flex items-center justify-center text-emerald-600">
                    <CreditCard className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-zinc-900 group-hover:text-emerald-600 transition-colors">
                      Quản lý gói cước
                    </h3>
                    <p className="text-xs text-zinc-500 mt-0.5">
                      Cập nhật tính năng &amp; biểu giá
                    </p>
                  </div>
                </div>
              </Card>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
