import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GoogleLogin } from "@react-oauth/google";
import toast from "../../../utils/toast";
import authApi from "../../../api/authApi";
import OtpInput from "./OtpInput";
import {
  Mail,
  Lock,
  User,
  ArrowRight,
  CheckCircle,
  AlertCircle,
  ArrowLeft,
  RotateCcw,
} from "lucide-react";
import logoImg from "../../../assets/images/vivu_logo-remove-background.com.png";

const RegisterForm = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [step, setStep] = useState(1);
  const [otp, setOtp] = useState("");
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
  });

  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [countdown, setCountdown] = useState(0);

  useEffect(() => {
    let timer;
    if (countdown > 0) {
      timer = setInterval(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [countdown]);

  // Validation rules
  const validate = (name, value) => {
    let error = "";
    switch (name) {
      case "fullName":
        if (!value) error = "Họ và tên là bắt buộc";
        else if (value.length > 100)
          error = "Họ và tên không được quá 100 ký tự";
        break;
      case "email":
        if (!value) error = "Email là bắt buộc";
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))
          error = "Email không hợp lệ";
        else if (value.length > 255) error = "Email không được quá 255 ký tự";
        break;
      case "password":
        if (!value) error = "Mật khẩu là bắt buộc";
        else {
          if (value.length < 8) error = "Mật khẩu phải có ít nhất 8 ký tự";
          else if (!/(?=.*[a-z])/.test(value))
            error = "Phải có ít nhất 1 chữ thường (a-z)";
          else if (!/(?=.*[A-Z])/.test(value))
            error = "Phải có ít nhất 1 chữ hoa (A-Z)";
          else if (!/(?=.*\d)/.test(value))
            error = "Phải có ít nhất 1 số (0-9)";
        }
        break;
      default:
        break;
    }
    return error;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));

    // Validate on change if touched
    if (touched[name]) {
      setErrors((prev) => ({ ...prev, [name]: validate(name, value) }));
    }
  };

  const handleBlur = (e) => {
    const { name, value } = e.target;
    setTouched((prev) => ({ ...prev, [name]: true }));
    setErrors((prev) => ({ ...prev, [name]: validate(name, value) }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Step 1: Validate Info and Send OTP
    if (step === 1) {
      // Validate all fields
      const newErrors = {};
      let isValid = true;
      Object.keys(formData).forEach((key) => {
        const error = validate(key, formData[key]);
        if (error) {
          newErrors[key] = error;
          isValid = false;
        }
      });
      setErrors(newErrors);
      setTouched({ fullName: true, email: true, password: true });

      if (isValid) {
        setLoading(true);
        try {
          await authApi.sendVerificationEmail({ email: formData.email });
          toast.success("Mã xác thực đã được gửi đến email của bạn!");
          setStep(2);
          setCountdown(60);
        } catch (error) {
          console.error("Send OTP error:", error);

          if (error.response?.data?.error?.code === "User.EmailAlreadyExists") {
            setErrors((prev) => ({ ...prev, email: "Email đã tồn tại" }));
          }

          toast.error(
            error.response?.data?.message ||
              error.response?.data?.error?.message ||
              "Gửi mã xác thực thất bại.",
          );
        } finally {
          setLoading(false);
        }
      }
    }
    // Step 2: Verify OTP and Register
    else if (step === 2) {
      if (!otp || otp.length !== 6) {
        toast.error("Vui lòng nhập mã xác thực 6 số.");
        return;
      }

      setLoading(true);
      try {
        const payload = {
          fullName: formData.fullName,
          email: formData.email,
          password: formData.password,
          otpCode: otp,
        };

        await authApi.register(payload);

        toast.success("Đăng ký thành công! Vui lòng đăng nhập.");
        navigate("/login");
      } catch (error) {
        console.error("Register error:", error);
        console.error("Register error:", error);
        const errorCode = error.response?.data?.error?.code;
        if (errorCode === "Auth.OtpInvalid") {
          toast.error("Mã OTP không chính xác.");
        } else if (errorCode === "Auth.OtpExpired") {
          toast.error("Mã OTP đã hết hạn.");
        } else {
          toast.error(
            error.response?.data?.message ||
              error.response?.data?.error?.message ||
              "Đăng ký thất bại. Mã xác thực không đúng hoặc đã hết hạn.",
          );
        }
      } finally {
        setLoading(false);
      }
    }
  };

  const handleResendOtp = async () => {
    if (countdown > 0) return;
    setLoading(true);
    try {
      await authApi.sendVerificationEmail({ email: formData.email });
      toast.success("Mã xác thực mới đã được gửi!");
      setCountdown(60);
    } catch (error) {
      console.error("Resend OTP error:", error);
      toast.error("Không thể gửi lại mã xác thực. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  // Helper function for Google login success
  const handleGoogleLoginSuccess = async (accessToken, refreshToken) => {
    // Save tokens directly to localStorage (reliable and synchronous)
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);

    try {
      const user = await authApi.getMe();
      localStorage.setItem("user", JSON.stringify(user));
    } catch (error) {
      console.error("Failed to fetch user info:", error);
    }

    toast.success("Đăng nhập Google thành công!");
    navigate("/chat");
  };

  return (
    <div className="flex-1 flex flex-col relative z-10 bg-white dark:bg-surface-dark transition-colors duration-300 lg:flex-none lg:w-1/2 xl:w-[45%]">
      <div className="flex-1 flex flex-col justify-center px-4 sm:px-6 lg:px-20 xl:px-24 overflow-y-auto">
        <div className="mx-auto w-full max-w-sm lg:w-96 py-12 lg:pt-32 lg:pb-20">
          {/* Logo */}
          <div className="mb-10 text-center lg:text-left">
            <Link className="inline-flex items-center gap-2 group" to="/">
              <img
                src={logoImg}
                alt="Vivu Logo"
                className="h-10 md:h-12 w-auto object-contain group-hover:scale-105 transition-transform duration-300"
              />
            </Link>
          </div>

          <div className="text-center lg:text-left mb-8">
            <h2 className="text-3xl font-extrabold text-text-main-light dark:text-white tracking-tight mb-2">
              Tạo tài khoản mới
            </h2>
            <p className="text-sm text-text-muted-light dark:text-text-muted-dark">
              Bắt đầu hành trình khám phá Việt Nam cùng Vivu.
            </p>
          </div>

          <div className="space-y-6">
            {/* Google Sign In */}
            <div>
              <div className="w-full relative group">
                <button
                  className="w-full inline-flex justify-center items-center py-3 px-4 rounded-xl shadow-sm bg-white dark:bg-surface-dark text-sm font-semibold text-text-main-light dark:text-white border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-all duration-200"
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
                  Đăng ký với Google
                </button>
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
                          await handleGoogleLoginSuccess(
                            accessToken,
                            refreshToken,
                          );
                        }
                      } catch (error) {
                        console.error("Google login error:", error);
                        toast.error("Đăng nhập Google thất bại.");
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
                  {step === 1 ? "Hoặc đăng ký bằng Email" : "Xác thực Email"}
                </span>
              </div>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
              {step === 1 && (
                <>
                  {/* Full Name Field */}
                  <div>
                    <label
                      className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5"
                      htmlFor="fullName"
                    >
                      Họ và tên
                    </label>
                    <div className="relative group">
                      <input
                        type="text"
                        id="fullName"
                        name="fullName"
                        value={formData.fullName}
                        onChange={handleChange}
                        onBlur={handleBlur}
                        placeholder="Nguyễn Văn A"
                        className={`appearance-none rounded-xl relative block w-full pl-10 pr-3 py-3 border bg-transparent focus:outline-none focus:ring-2 sm:text-sm transition-all duration-200 ${
                          errors.fullName
                            ? "border-red-300 dark:border-red-700 text-red-900 dark:text-red-100 placeholder-red-300 focus:ring-red-500 focus:border-red-500"
                            : "border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white focus:ring-primary focus:border-primary hover:border-gray-400 dark:hover:border-gray-500"
                        }`}
                      />
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <User
                          className={`h-5 w-5 transition-colors ${errors.fullName ? "text-red-500" : "text-gray-400 group-focus-within:text-primary"}`}
                        />
                      </div>
                      {errors.fullName && (
                        <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                          <AlertCircle className="h-5 w-5 text-red-500" />
                        </div>
                      )}
                    </div>
                    {errors.fullName && (
                      <p className="mt-1 text-xs text-red-500">
                        {errors.fullName}
                      </p>
                    )}
                  </div>

                  {/* Email Field */}
                  <div>
                    <label
                      className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5"
                      htmlFor="email"
                    >
                      Địa chỉ Email
                    </label>
                    <div className="relative group">
                      <input
                        type="email"
                        id="email"
                        name="email"
                        value={formData.email}
                        onChange={handleChange}
                        onBlur={handleBlur}
                        placeholder="you@example.com"
                        autoComplete="email"
                        className={`appearance-none rounded-xl relative block w-full pl-10 pr-3 py-3 border bg-transparent focus:outline-none focus:ring-2 sm:text-sm transition-all duration-200 ${
                          errors.email
                            ? "border-red-300 dark:border-red-700 text-red-900 dark:text-red-100 placeholder-red-300 focus:ring-red-500 focus:border-red-500"
                            : "border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white focus:ring-primary focus:border-primary hover:border-gray-400 dark:hover:border-gray-500"
                        }`}
                      />
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Mail
                          className={`h-5 w-5 transition-colors ${errors.email ? "text-red-500" : "text-gray-400 group-focus-within:text-primary"}`}
                        />
                      </div>
                      {errors.email && (
                        <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                          <AlertCircle className="h-5 w-5 text-red-500" />
                        </div>
                      )}
                    </div>
                    {errors.email && (
                      <p className="mt-1 text-xs text-red-500">
                        {errors.email}
                      </p>
                    )}
                  </div>

                  {/* Password Field */}
                  <div>
                    <label
                      className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5"
                      htmlFor="password"
                    >
                      Mật khẩu
                    </label>
                    <div className="relative group">
                      <input
                        type="password"
                        id="password"
                        name="password"
                        value={formData.password}
                        onChange={handleChange}
                        onBlur={handleBlur}
                        placeholder="••••••••"
                        autoComplete="new-password"
                        className={`appearance-none rounded-xl relative block w-full pl-10 pr-3 py-3 border bg-transparent focus:outline-none focus:ring-2 sm:text-sm transition-all duration-200 ${
                          errors.password
                            ? "border-red-300 dark:border-red-700 text-red-900 dark:text-red-100 placeholder-red-300 focus:ring-red-500 focus:border-red-500"
                            : "border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white focus:ring-primary focus:border-primary hover:border-gray-400 dark:hover:border-gray-500"
                        }`}
                      />
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Lock
                          className={`h-5 w-5 transition-colors ${errors.password ? "text-red-500" : "text-gray-400 group-focus-within:text-primary"}`}
                        />
                      </div>
                      {errors.password && (
                        <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                          <AlertCircle className="h-5 w-5 text-red-500" />
                        </div>
                      )}
                    </div>
                    <div className="mt-3 text-xs space-y-1.5 text-text-muted-light dark:text-text-muted-dark grid grid-cols-2 gap-x-2">
                      <p
                        className={
                          formData.password.length >= 8
                            ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                            : "flex items-center gap-1.5"
                        }
                      >
                        {formData.password.length >= 8 ? (
                          <CheckCircle className="w-3.5 h-3.5" />
                        ) : (
                          <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
                        )}
                        Tối thiểu 8 ký tự
                      </p>
                      <p
                        className={
                          /[A-Z]/.test(formData.password)
                            ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                            : "flex items-center gap-1.5"
                        }
                      >
                        {/[A-Z]/.test(formData.password) ? (
                          <CheckCircle className="w-3.5 h-3.5" />
                        ) : (
                          <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
                        )}
                        Ít nhất 1 chữ hoa
                      </p>
                      <p
                        className={
                          /[a-z]/.test(formData.password)
                            ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                            : "flex items-center gap-1.5"
                        }
                      >
                        {/[a-z]/.test(formData.password) ? (
                          <CheckCircle className="w-3.5 h-3.5" />
                        ) : (
                          <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
                        )}
                        Ít nhất 1 chữ thường
                      </p>
                      <p
                        className={
                          /\d/.test(formData.password)
                            ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                            : "flex items-center gap-1.5"
                        }
                      >
                        {/\d/.test(formData.password) ? (
                          <CheckCircle className="w-3.5 h-3.5" />
                        ) : (
                          <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
                        )}
                        Ít nhất 1 số
                      </p>
                    </div>
                  </div>
                </>
              )}

              {step === 2 && (
                <div className="space-y-4">
                  <div className="text-center">
                    <p className="text-sm text-text-muted-light dark:text-text-muted-dark mb-4">
                      Chúng tôi đã gửi mã xác thực 6 số đến{" "}
                      <strong>{formData.email}</strong>.<br />
                      Vui lòng nhập mã để hoàn tất đăng ký.
                    </p>
                    <OtpInput length={6} onComplete={(code) => setOtp(code)} />
                  </div>
                  <div className="flex items-center justify-center gap-4">
                    <button
                      type="button"
                      onClick={() => setStep(1)}
                      className="text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 font-medium flex items-center gap-1 transition-colors"
                    >
                      <ArrowLeft className="w-4 h-4" /> Quay lại
                    </button>

                    <span className="text-gray-300 dark:text-gray-700">|</span>

                    <button
                      type="button"
                      disabled={countdown > 0 || loading}
                      onClick={handleResendOtp}
                      className={`text-sm font-medium flex items-center gap-1 transition-colors ${
                        countdown > 0
                          ? "text-gray-400 cursor-not-allowed"
                          : "text-primary hover:text-primary-dark"
                      }`}
                    >
                      <RotateCcw
                        className={`w-4 h-4 ${loading ? "animate-spin" : ""}`}
                      />
                      {countdown > 0
                        ? `Gửi lại sau (${countdown}s)`
                        : "Gửi lại mã"}
                    </button>
                  </div>
                </div>
              )}

              <div className="pt-4">
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
                      {step === 1 ? "Tiếp tục" : "Xác nhận & Đăng ký"}
                      <ArrowRight className="ml-2 h-4 w-4 group-hover:translate-x-1 transition-transform" />
                    </>
                  )}
                </button>
              </div>
            </form>

            <div className="text-center text-sm pt-4">
              <span className="text-text-muted-light dark:text-text-muted-dark">
                Đã có tài khoản?{" "}
              </span>
              <Link
                className="font-semibold text-primary hover:text-primary-dark transition-colors"
                to="/login"
              >
                Đăng nhập
              </Link>
            </div>
          </div>

          <div className="mt-8 pt-6 border-t border-gray-100 dark:border-gray-800 text-xs text-center text-text-muted-light dark:text-text-muted-dark">
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

export default RegisterForm;
