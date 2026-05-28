import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { GoogleLogin } from "@react-oauth/google";
import toast from "../../../utils/toast";
import { Mail, Lock, ArrowRight } from "lucide-react";
import authApi from "../../../api/authApi";
import { useAuth } from "../../../contexts/auth-context";
import logoImg from "../../../assets/images/vivu_logo-remove-background.com.png";
import { getRolesFromToken } from "../../../utils/getRolesFromToken";

const LoginForm = () => {
  const navigate = useNavigate();
  const auth = useAuth();
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({
    email: "",
    password: "",
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  // Helper function to handle successful login
  const handleLoginSuccess = async (accessToken, refreshToken) => {
    // Save tokens directly to localStorage (reliable and synchronous)
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);

    // Fetch current user info
    try {
      const userResponse = await authApi.getMe();
      const userData = userResponse.data || userResponse;

      // Save user data
      localStorage.setItem("user", JSON.stringify(userData));

      // Use AuthContext to update global state
      auth.login(accessToken, refreshToken, userData);

      // Check for CMS access and redirect
      const userRoles = getRolesFromToken(accessToken).map((r) => r.toUpperCase());
      if (userRoles.includes("ADMIN")) {
        toast.success("Đăng nhập thành công!");
        navigate("/cms/dashboard");
        return;
      }
      if (userRoles.includes("MODERATOR")) {
        toast.success("Đăng nhập thành công!");
        navigate("/cms/cities");
        return;
      }
    } catch (error) {
      console.error("Failed to fetch user info:", error);
    }

    toast.success("Đăng nhập thành công!");
    navigate("/chat");
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      const { email, password } = formData;
      const response = await authApi.login({ email, password });

      if (response.success && response.data) {
        const { accessToken, refreshToken } = response.data;
        await handleLoginSuccess(accessToken, refreshToken);
      }
    } catch (error) {
      console.error("Login process error:", error);
      const errMsg = error.response?.data?.message || error.message || "";
      if (errMsg.includes("Banned") || errMsg.includes("banned")) {
        toast.error("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
      } else {
        toast.error(
          error.response?.data?.message ||
            "Đăng nhập thất bại. Vui lòng thử lại.",
        );
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col relative z-10 bg-white dark:bg-surface-dark transition-colors duration-300 lg:flex-none lg:w-1/2 xl:w-[45%]">
      <div className="flex-1 flex flex-col justify-center px-4 sm:px-6 lg:px-20 xl:px-24 overflow-y-auto">
        <div className="mx-auto w-full max-w-sm lg:w-96 py-8 lg:py-10">
          {/* Logo */}
          <div className="mb-6 text-center lg:text-left">
            <Link className="inline-flex items-center gap-2 group" to="/">
              <img
                src={logoImg}
                alt="Vivu Logo"
                className="h-10 md:h-12 w-auto object-contain group-hover:scale-105 transition-transform duration-300"
              />
            </Link>
          </div>

          <div className="text-center lg:text-left mb-6">
            <h2 className="text-3xl font-extrabold text-text-main-light dark:text-white tracking-tight mb-2">
              Chào mừng trở lại
            </h2>
            <p className="text-sm text-text-muted-light dark:text-text-muted-dark">
              Vui lòng nhập thông tin chi tiết để đăng nhập.
            </p>
            {location.state?.banned && (
              <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-700 dark:text-red-400 font-medium">
                ⛔ Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.
              </div>
            )}
          </div>

          <div className="space-y-5">
            <div>
              <div className="w-full relative group">
                {/* Visual Custom Button */}
                <button
                  className="w-full inline-flex justify-center items-center py-3 px-4 rounded-xl shadow-sm bg-white dark:bg-surface-dark text-sm font-semibold text-text-main-light dark:text-white border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-600 transition-all duration-200"
                  type="button"
                >
                  <svg
                    aria-hidden="true"
                    className="h-5 w-5 mr-3"
                    viewBox="0 0 24 24"
                  >
                    <path
                      d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                      fill="#4285F4"
                    ></path>
                    <path
                      d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                      fill="#34A853"
                    ></path>
                    <path
                      d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                      fill="#FBBC05"
                    ></path>
                    <path
                      d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                      fill="#EA4335"
                    ></path>
                  </svg>
                  Đăng nhập với Google
                </button>

                {/* Invisible Google Button Overlay */}
                <div className="absolute inset-0 opacity-0 z-10 overflow-hidden">
                  <GoogleLogin
                    onSuccess={async (credentialResponse) => {
                      try {
                        setLoading(true);
                        const response = await authApi.googleLogin({
                          idToken: credentialResponse.credential,
                        });

                        if (response.success && response.data) {
                          const { accessToken, refreshToken } = response.data;
                          await handleLoginSuccess(accessToken, refreshToken);
                        }
                      } catch (error) {
                        console.error("Google login error:", error);
                        const errMsg = error.response?.data?.message || error.message || "";
                        if (errMsg.includes("Banned") || errMsg.includes("banned")) {
                          toast.error("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                        } else {
                          toast.error("Đăng nhập Google thất bại.");
                        }
                      } finally {
                        setLoading(false);
                      }
                    }}
                    onError={() => {
                      toast.error("Đăng nhập Google thất bại.");
                    }}
                    useOneTap={false}
                    width="500"
                  />
                </div>
              </div>
            </div>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-200 dark:border-gray-700"></div>
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-4 bg-white dark:bg-surface-dark text-text-muted-light dark:text-text-muted-dark font-medium">
                  Hoặc đăng nhập bằng Email
                </span>
              </div>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="space-y-4">
                <div>
                  <label
                    className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5"
                    htmlFor="email-address"
                  >
                    Địa chỉ Email
                  </label>
                  <div className="relative group">
                    <input
                      autoComplete="email"
                      className="appearance-none rounded-xl relative block w-full pl-10 pr-3 py-3 border border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white bg-transparent focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent sm:text-sm transition-all duration-200 hover:border-gray-400 dark:hover:border-gray-500"
                      id="email-address"
                      name="email"
                      value={formData.email}
                      onChange={handleChange}
                      placeholder="you@example.com"
                      required
                      type="email"
                    />
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Mail className="h-5 w-5 text-gray-400 group-focus-within:text-primary transition-colors" />
                    </div>
                  </div>
                </div>
                <div>
                  <div className="flex items-center justify-between mb-1.5">
                    <label
                      className="block text-sm font-semibold text-text-main-light dark:text-white"
                      htmlFor="password"
                    >
                      Mật khẩu
                    </label>
                    <div className="text-sm">
                      <Link
                        className="font-medium text-primary hover:text-primary-dark transition-colors"
                        to="/forgot-password"
                      >
                        Quên mật khẩu?
                      </Link>
                    </div>
                  </div>
                  <div className="relative group">
                    <input
                      autoComplete="current-password"
                      className="appearance-none rounded-xl relative block w-full pl-10 pr-3 py-3 border border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white bg-transparent focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent sm:text-sm transition-all duration-200 hover:border-gray-400 dark:hover:border-gray-500"
                      id="password"
                      name="password"
                      value={formData.password}
                      onChange={handleChange}
                      placeholder="••••••••"
                      required
                      type="password"
                    />
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Lock className="h-5 w-5 text-gray-400 group-focus-within:text-primary transition-colors" />
                    </div>
                  </div>
                </div>
              </div>

              <div className="flex items-center">
                <input
                  className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded cursor-pointer"
                  id="remember-me"
                  name="remember-me"
                  type="checkbox"
                />
                <label
                  className="ml-2 block text-sm text-text-muted-light dark:text-text-muted-dark cursor-pointer select-none"
                  htmlFor="remember-me"
                >
                  Ghi nhớ trong 30 ngày
                </label>
              </div>

              <div>
                <button
                  disabled={loading}
                  className={`group relative w-full flex justify-center py-3.5 px-4 border border-transparent text-sm font-bold rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-all duration-300 shadow-lg shadow-blue-500/30 hover:shadow-blue-500/50 hover:-translate-y-0.5 bg-gradient-primary ${loading ? "opacity-70 cursor-not-allowed" : "hover:opacity-90"}`}
                  type="submit"
                >
                  {loading ? (
                    <svg
                      className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
                      xmlns="http://www.w3.org/2000/svg"
                      fill="none"
                      viewBox="0 0 24 24"
                    >
                      <circle
                        className="opacity-25"
                        cx="12"
                        cy="12"
                        r="10"
                        stroke="currentColor"
                        strokeWidth="4"
                      ></circle>
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                      ></path>
                    </svg>
                  ) : (
                    <>
                      Đăng nhập
                      <ArrowRight className="ml-2 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                    </>
                  )}
                </button>
              </div>
            </form>

            <div className="text-center text-sm">
              <span className="text-text-muted-light dark:text-text-muted-dark">
                Chưa có tài khoản?{" "}
              </span>
              <Link
                className="font-semibold text-primary hover:text-primary-dark transition-colors"
                to="/register"
              >
                Đăng ký ngay
              </Link>
            </div>
          </div>

          <div className="mt-6 pt-4 border-t border-gray-100 dark:border-gray-800 text-xs text-center text-text-muted-light dark:text-text-muted-dark">
            <p>© 2026 Vivu. All rights reserved.</p>
            <div className="flex justify-center gap-4 mt-2">
              <a className="hover:text-primary transition-colors" href="#">
                Quyền riêng tư
              </a>
              <a className="hover:text-primary transition-colors" href="#">
                Điều khoản
              </a>
              <a className="hover:text-primary transition-colors" href="#">
                Trợ giúp
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginForm;
