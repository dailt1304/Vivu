import React from "react";
import RegisterForm from "../../../components/user/auth/RegisterForm";
import TravelCarousel from "../../../components/user/auth/TravelCarousel";

/**
 * Register Page Component
 * Main container that acts as a layout for RegisterForm and TravelCarousel.
 * Reuses the same split-screen layout as LoginPage.
 */
const RegisterPage = () => {
  return (
    <div className="h-screen w-full flex overflow-hidden bg-surface-light dark:bg-background-dark">
      <RegisterForm />
      <TravelCarousel />
    </div>
  );
};

export default RegisterPage;
