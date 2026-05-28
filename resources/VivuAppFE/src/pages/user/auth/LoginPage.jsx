import React from "react";
import LoginForm from "../../../components/user/auth/LoginForm";
import TravelCarousel from "../../../components/user/auth/TravelCarousel";

/**
 * Login Page Component
 * Main container that acts as a layout for LoginForm and TravelCarousel.
 */
const LoginPage = () => {
  return (
    <div className="h-screen w-full flex overflow-hidden bg-surface-light dark:bg-background-dark">
      <LoginForm />
      <TravelCarousel />
    </div>
  );
};

export default LoginPage;
