import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/auth-context";
import LandingPage from "../pages/user/LandingPage";

const HomeRedirect = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return null;

  if (isAuthenticated) return <Navigate to="/chat" replace />;

  return <LandingPage />;
};

export default HomeRedirect;
