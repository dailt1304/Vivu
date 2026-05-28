import React from "react";
import { Navigate, Outlet } from "react-router-dom";
import BottomNavbar from "../../layout/BottomNavbar";

/**
 * ProtectedRoute - Checks authentication before rendering child routes
 * Uses direct localStorage check for reliability during navigation
 */
const ProtectedRoute = () => {
  // Read directly from localStorage for immediate sync
  // This avoids race conditions with React state updates
  const token = localStorage.getItem("access_token");

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return (
    <>
      <Outlet />
      <BottomNavbar />
    </>
  );
};

export default ProtectedRoute;
