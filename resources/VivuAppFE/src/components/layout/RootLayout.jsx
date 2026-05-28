import { Suspense } from "react";
import { Outlet } from "react-router-dom";
import { AuthProvider } from "../../contexts/AuthContext";
import { NotificationProvider } from "../../contexts/NotificationContext";
import { SwrProvider } from "../../lib/swr";
import GenerationTracker from "../global/GenerationTracker";

// Loading fallback for lazy-loaded routes
const PageLoader = () => (
  <div className="flex items-center justify-center min-h-screen">
    <div className="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin" />
  </div>
);

/**
 * RootLayout - Wraps the entire app with providers
 * This is used with createBrowserRouter to provide context
 */
function RootLayout() {
  return (
    <AuthProvider>
      <NotificationProvider>
        <SwrProvider>
          <Suspense fallback={<PageLoader />}>
            <Outlet />
            <GenerationTracker />
          </Suspense>
        </SwrProvider>
      </NotificationProvider>
    </AuthProvider>
  );
}

export default RootLayout;
