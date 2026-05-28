import { createContext, useContext } from "react";

export const AuthContext = createContext(null);

/**
 * useAuth hook - access auth state from any component
 * Must be used within AuthProvider
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
};
