import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import CMSLayout from "./layout/CMSLayout";
import { useAuth } from "../../contexts/auth-context";

const CMSProtectedRoute = ({ allowedRoles = ["ADMIN", "MODERATOR"] }) => {
  const location = useLocation();
  const { isAuthenticated, user, isLoading } = useAuth();
  const userRoles = (user?.roles || []).map((role) => role.toUpperCase());
  const normalizedAllowedRoles = allowedRoles.map((role) => role.toUpperCase());

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  // Check if user is authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check if user has required role
  const hasAllowedRole = normalizedAllowedRoles.some((role) =>
    userRoles.includes(role),
  );
  if (!hasAllowedRole) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50">
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">403</h1>
          <p className="text-gray-600 mb-4">
            Bạn không có quyền truy cập trang này
          </p>
          <a href="/" className="text-primary hover:underline">
            Quay về trang chủ
          </a>
        </div>
      </div>
    );
  }

  // Admin-only routes check
  const adminOnlyPaths = ["/cms/dashboard", "/cms/users", "/cms/subscriptions"];
  if (
    adminOnlyPaths.includes(location.pathname) &&
    !userRoles.includes("ADMIN")
  ) {
    return <Navigate to="/cms/locations" replace />;
  }

  return <CMSLayout userRoles={userRoles} />;
};

export default CMSProtectedRoute;
