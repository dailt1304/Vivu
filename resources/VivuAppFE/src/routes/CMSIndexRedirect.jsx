import React from "react";
import { Navigate } from "react-router-dom";
import { getRolesFromToken } from "../utils/getRolesFromToken";

/**
 * Smart redirect for /cms index route.
 * - ADMIN → /cms/dashboard
 * - MODERATOR → /cms/cities
 */
const CMSIndexRedirect = () => {
  const token = localStorage.getItem("access_token");
  const roles = token
    ? getRolesFromToken(token).map((r) => r.toUpperCase())
    : [];

  if (roles.includes("ADMIN")) {
    return <Navigate to="/cms/dashboard" replace />;
  }

  return <Navigate to="/cms/cities" replace />;
};

export default CMSIndexRedirect;
