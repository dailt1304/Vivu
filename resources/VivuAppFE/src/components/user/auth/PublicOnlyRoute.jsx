import React from "react";
import { Navigate, Outlet } from "react-router-dom";

/**
 * PublicOnlyRoute - Redirects authenticated users away from public-only pages (login, register, etc.)
 * Uses direct localStorage check for reliability during navigation
 */
const PublicOnlyRoute = () => {
  // Read directly from localStorage for immediate sync
  // This avoids race conditions with React state updates during navigation
  const token = localStorage.getItem("access_token");

  if (token) {
    // If user is already logged in, redirect them to the main app area
    return <Navigate to="/chat" replace />;
  }

  // If not logged in, allow access to the public-only pages (login, register, etc.)
  return <Outlet />;
};

export default PublicOnlyRoute;
