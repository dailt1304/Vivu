import { lazy } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";

import ProtectedRoute from "../components/user/auth/ProtectedRoute";
import PublicOnlyRoute from "../components/user/auth/PublicOnlyRoute";
import CMSProtectedRoute from "../components/cms/CMSProtectedRoute";
import ErrorBoundary from "../components/common/ErrorBoundary";
import RootLayout from "../components/layout/RootLayout";
import HomeRedirect from "./HomeRedirect";
import CMSIndexRedirect from "./CMSIndexRedirect";

// Lazy-loaded pages (code splitting)
const LandingPage = lazy(() => import("../pages/user/LandingPage"));
const LoginPage = lazy(() => import("../pages/user/auth/LoginPage"));
const RegisterPage = lazy(() => import("../pages/user/auth/RegisterPage"));
const ForgotPasswordPage = lazy(
  () => import("../pages/user/auth/ForgotPasswordPage"),
);
const ChatPage = lazy(() => import("../pages/user/chat/ChatPage"));
const MyTripsPage = lazy(() => import("../pages/user/trips/MyTripsPage"));
const TripDetailPage = lazy(() => import("../pages/user/trips/TripDetailPage"));
const JoinTripPage = lazy(() => import("../pages/user/trips/JoinTripPage"));
const PublicTripDetailPage = lazy(
  () => import("../pages/user/trips/PublicTripDetailPage"),
);
const ExplorePage = lazy(() => import("../pages/user/explore/ExplorePage"));
const SavedPage = lazy(() => import("../pages/user/saved/SavedPage"));
const CollectionDetailPage = lazy(
  () => import("../pages/user/saved/CollectionDetailPage"),
);
const InspirationPage = lazy(
  () => import("../pages/user/blog/InspirationPage"),
);
const CreateBlogPage = lazy(() => import("../pages/user/blog/CreateBlogPage"));
const MyBlogsPage = lazy(() => import("../pages/user/blog/MyBlogsPage"));
const BlogDetailPage = lazy(() => import("../pages/user/blog/BlogDetailPage"));

const SettingsPage = lazy(() => import("../pages/user/settings/SettingsPage"));
const SubscriptionPage = lazy(
  () => import("../pages/user/subscription/SubscriptionPage"),
);
const PricingPage = lazy(() => import("../pages/user/pricing/PricingPage"));
const PaymentSuccessPage = lazy(
  () => import("../pages/user/payment/PaymentSuccessPage"),
);
const PaymentCancelPage = lazy(
  () => import("../pages/user/payment/PaymentCancelPage"),
);

// CMS Pages (lazy loaded)
const DashboardPage = lazy(() => import("../pages/cms/admin/DashboardPage"));
const UserManagementPage = lazy(
  () => import("../pages/cms/admin/UserManagementPage"),
);
const SubscriptionPlansPage = lazy(
  () => import("../pages/cms/admin/SubscriptionPlansPage"),
);
const BlogManagementPage = lazy(
  () => import("../pages/cms/moderator/BlogManagementPage"),
);
const CityManagementPage = lazy(
  () => import("../pages/cms/moderator/CityManagementPage"),
);
const LocationManagementPage = lazy(
  () => import("../pages/cms/moderator/LocationManagementPage"),
);
const CustomerSupportPage = lazy(
  () => import("../pages/cms/moderator/CustomerSupportPage"),
);
const LocationCategoryManagementPage = lazy(
  () => import("../pages/cms/moderator/LocationCategoryManagementPage"),
);
const CMSProfilePage = lazy(
  () => import("../pages/cms/common/CMSProfilePage"),
);

export const router = createBrowserRouter([
  {
    // Root layout wraps all routes with AuthProvider
    element: <RootLayout />,
    errorElement: <ErrorBoundary />,
    children: [
      {
        path: "/",
        element: <HomeRedirect />,
      },
      {
        path: "/pricing",
        element: <PricingPage />,
      },
      {
        path: "/join/:inviteCode",
        element: <JoinTripPage />,
      },
      {
        path: "/trips/public/:id",
        element: <PublicTripDetailPage />,
      },
      {
        path: "/payment/success",
        element: <PaymentSuccessPage />,
      },
      {
        path: "/payment/cancel",
        element: <PaymentCancelPage />,
      },
      {
        element: <PublicOnlyRoute />,
        children: [
          {
            path: "/login",
            element: <LoginPage />,
          },
          {
            path: "/register",
            element: <RegisterPage />,
          },
          {
            path: "/forgot-password",
            element: <ForgotPasswordPage />,
          },
        ],
      },
      {
        element: <ProtectedRoute />,
        children: [
          {
            path: "/chat",
            element: <ChatPage />,
          },
          {
            path: "/my-trips",
            element: <MyTripsPage />,
          },
          {
            path: "/trips/:id",
            element: <TripDetailPage />,
          },
          {
            path: "/explore",
            element: <ExplorePage />,
          },
          {
            path: "/saved",
            element: <SavedPage />,
          },
          {
            path: "/saved/:id",
            element: <CollectionDetailPage />,
          },
          {
            path: "/inspiration",
            element: <InspirationPage />,
          },
          {
            path: "/inspiration/create",
            element: <CreateBlogPage />,
          },
          {
            path: "/inspiration/edit/:id",
            element: <CreateBlogPage />,
          },
          {
            path: "/inspiration/me",
            element: <MyBlogsPage />,
          },
          {
            path: "/inspiration/:id",
            element: <BlogDetailPage />,
          },
          {
            path: "/profile",
            element: <SettingsPage />,
          },
          {
            path: "/settings",
            element: <Navigate to="/profile" replace />,
          },
          {
            path: "/subscription",
            element: <SubscriptionPage />,
          },

        ],
      },
      // CMS Routes
      {
        path: "/cms",
        element: <CMSProtectedRoute allowedRoles={["ADMIN", "MODERATOR"]} />,
        children: [
          // Redirect /cms based on role: Admin → dashboard, Moderator → cities
          {
            index: true,
            element: <CMSIndexRedirect />,
          },
          // Admin only pages
          {
            path: "dashboard",
            element: <DashboardPage />,
          },
          {
            path: "users",
            element: <UserManagementPage />,
          },
          {
            path: "subscriptions",
            element: <SubscriptionPlansPage />,
          },
          // Moderator pages (accessible by both Admin and Moderator)
          {
            path: "blogs",
            element: <BlogManagementPage />,
          },
          {
            path: "cities",
            element: <CityManagementPage />,
          },
          {
            path: "locations",
            element: <LocationManagementPage />,
          },
          {
            path: "categories",
            element: <LocationCategoryManagementPage />,
          },
          {
            path: "support",
            element: <CustomerSupportPage />,
          },
          {
            path: "profile",
            element: <CMSProfilePage />,
          },
        ],
      },
    ],
  },
]);

export default router;
