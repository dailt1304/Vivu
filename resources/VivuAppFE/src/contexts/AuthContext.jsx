import { useState, useEffect, useCallback, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "./auth-context";
import toast from "../utils/toast";
import { getRolesFromToken } from "../utils/getRolesFromToken";

const attachJwtRoles = (userData, overrideToken = null) => {
  if (!userData) return userData;
  const token = overrideToken || localStorage.getItem("access_token");
  const jwtRoles = getRolesFromToken(token);
  return { ...userData, roles: jwtRoles };
};

/**
 * AuthProvider - Single source of truth for authentication state
 * Wraps the entire app to provide auth state to all components
 */
export function AuthProvider({ children }) {
  const navigate = useNavigate();
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem("user");
      if (stored) {
        const parsed = JSON.parse(stored);
        const rawUser = parsed?.data || parsed;
        return attachJwtRoles(rawUser);
      }
    } catch (e) {
      console.warn("Failed to parse initial user", e);
    }
    return null;
  });
  
  const [accessToken, setAccessToken] = useState(() => {
    return localStorage.getItem("access_token") || null;
  });
  
  const [isLoading, setIsLoading] = useState(() => {
    return !!localStorage.getItem("access_token");
  });

  // Initialize from localStorage on mount (fetch fresh data)
  useEffect(() => {
    let cancelled = false;

    const initAuth = async () => {
      try {
        const token = localStorage.getItem("access_token");
        const storedUser = localStorage.getItem("user");

        if (token) {
          try {
            const { default: authApi } = await import("../api/authApi");
            if (cancelled) return;

            const response = await authApi.getMe();
            if (cancelled) return;

            if (response.success && response.data) {
              const freshUser = response.data;

              // ✅ SECURITY FIX: Force logout if user is banned
              if (freshUser.status === "banned") {
                console.warn("User is banned, forcing logout.");
                localStorage.removeItem("access_token");
                localStorage.removeItem("refresh_token");
                localStorage.removeItem("user");
                setUser(null);
                setAccessToken(null);
                toast.error("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                setTimeout(() => {
                  navigate("/login", { state: { banned: true } });
                }, 0);
                return;
              }

              setUser(attachJwtRoles(freshUser, token));
              localStorage.setItem("user", JSON.stringify(freshUser));
            } else if (storedUser) {
              const parsed = JSON.parse(storedUser);
              const rawUser = parsed?.data || parsed;
              setUser(attachJwtRoles(rawUser, token));
            }
          } catch (apiError) {
            if (cancelled) return;
            // ✅ SECURITY FIX: Catch 403 banned response from getMe
            const status = apiError?.response?.status;
            const msg = apiError?.response?.data?.message || "";
            if (status === 403 && msg.includes("Banned")) {
              console.warn("User is banned (403), forcing logout.");
              localStorage.removeItem("access_token");
              localStorage.removeItem("refresh_token");
              localStorage.removeItem("user");
              setUser(null);
              setAccessToken(null);
              toast.error("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
              setTimeout(() => {
                navigate("/login", { state: { banned: true } });
              }, 0);
              return;
            }
            console.warn("Failed to refresh user on mount:", apiError);
            if (storedUser) {
              const parsed = JSON.parse(storedUser);
              const rawUser = parsed?.data || parsed;
              setUser(attachJwtRoles(rawUser, token));
            }
          }
        } else if (storedUser) {
          const parsed = JSON.parse(storedUser);
          const rawUser = parsed?.data || parsed;
          setUser(attachJwtRoles(rawUser, token));
        }
      } catch (error) {
        console.warn("Error reading auth from localStorage:", error);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    initAuth();

    // ✅ Cleanup: cancel on StrictMode remount to avoid double API call
    return () => {
      cancelled = true;
    };
  }, []);

  // Login - saves tokens and user data, updates state for all components
  const login = useCallback((token, refreshToken, userData) => {
    localStorage.setItem("access_token", token);
    localStorage.setItem("refresh_token", refreshToken);
    if (userData) {
      localStorage.setItem("user", JSON.stringify(userData));
      const rawUser = userData?.data || userData;
      setUser(attachJwtRoles(rawUser, token));
    }
    setAccessToken(token);
  }, []);

  // Logout - clears all auth data and redirects
  const logout = useCallback(() => {
    localStorage.removeItem("access_token");
    localStorage.removeItem("refresh_token");
    localStorage.removeItem("user");
    // Vercel Rule: rerender-functional-setstate
    setAccessToken(() => null);
    setUser(() => null);
    navigate("/login");
  }, [navigate]);

  // Refresh user data (e.g., after profile update)
  const refreshUser = useCallback((userData) => {
    localStorage.setItem("user", JSON.stringify(userData));
    const rawUser = userData?.data || userData;
    setUser(attachJwtRoles(rawUser));
  }, []);

  // Extract userId from user object
  const userId = useMemo(() => {
    if (!user) return null;
    return user?.id || user?.userId || null;
  }, [user]);

  // Memoize context value to prevent unnecessary re-renders
  const value = useMemo(
    () => ({
      user,
      userId,
      isAuthenticated: !!accessToken,
      isLoading,
      login,
      logout,
      refreshUser,
    }),
    [user, userId, accessToken, isLoading, login, logout, refreshUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export default AuthContext;
