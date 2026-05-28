import { createContext, useContext } from "react";

export const NotificationContext = createContext(null);

/**
 * useNotifications hook — access notification state from any component
 * Must be used within NotificationProvider
 */
export const useNotifications = () => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error("useNotifications must be used within NotificationProvider");
  }
  return context;
};
