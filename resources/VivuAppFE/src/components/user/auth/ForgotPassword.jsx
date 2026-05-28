import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Mail,
  ArrowLeft,
  Key,
  Lock,
  CheckCircle,
  Smartphone,
  RotateCcw,
} from "lucide-react";
import toast from "../../../utils/toast";
import authApi from "../../../api/authApi";
import OtpInput from "./OtpInput";

const ForgotPassword = () => {
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState("");
  const [otp, setOtp] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
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

  // Step 1: Request OTP
  const handleRequestOtp = async (e) => {
    e.preventDefault();
    if (!email) {
      toast.error("Vui lòng nhập địa chỉ email.");
      return;
    }

    setLoading(true);
    try {
      await authApi.forgotPassword({ email });
      setStep(2);
      setCountdown(60);
      toast.success("Mã xác thực đã được gửi đến email của bạn.");
    } catch (error) {
      console.error("Forgot password error:", error);
      toast.error(
        error.response?.data?.message ||
          error.response?.data?.error?.message ||
          "Không thể gửi mã xác thực. Vui lòng kiểm tra lại email.",
      );
    } finally {
      setLoading(false);
    }
  };

  const handleResendOtp = async () => {
    if (countdown > 0) return;
    setLoading(true);
    try {
      await authApi.forgotPassword({ email });
      toast.success("Mã xác thực mới đã được gửi!");
      setCountdown(60);
    } catch (error) {
      console.error("Resend OTP error:", error);
      toast.error("Không thể gửi lại mã xác thực. Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  // Step 2: Verify OTP
  const handleVerifyOtp = async (e) => {
    e.preventDefault();
    if (!otp || otp.length !== 6) {
      toast.error("Vui lòng nhập mã OTP 6 số.");
      return;
    }

    setLoading(true);
    try {
      await authApi.verifyResetOtp({ email, code: otp });
      setStep(3);
      toast.success("Xác thực thành công. Vui lòng đặt mật khẩu mới.");
    } catch (error) {
      console.error("Verify OTP error:", error);
      const errorCode = error.response?.data?.error?.code;
      if (errorCode === "Auth.OtpInvalid") {
        toast.error("Mã OTP không chính xác.");
      } else if (errorCode === "Auth.OtpExpired") {
        toast.error("Mã OTP đã hết hạn.");
      } else {
        toast.error(
          error.response?.data?.message ||
            error.response?.data?.error?.message ||
            "Xác thực thất bại.",
        );
      }
    } finally {
      setLoading(false);
    }
  };

  // Step 3: Reset Password
  const handleResetPassword = async (e) => {
    e.preventDefault();
    if (!password) {
      toast.error("Vui lòng nhập mật khẩu mới.");
      return;
    }
    if (password !== confirmPassword) {
      toast.error("Mật khẩu xác nhận không khớp.");
      return;
    }
    if (password.length < 6) {
      toast.error("Mật khẩu phải có ít nhất 6 ký tự.");
      return;
    }

    setLoading(true);
    try {
      await authApi.resetPassword({ email, newPassword: password });
      toast.success("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.");
      navigate("/login");
    } catch (error) {
      console.error("Reset password error:", error);
      toast.error(
        error.response?.data?.message ||
          error.response?.data?.error?.message ||
          "Đặt lại mật khẩu thất bại.",
      );
    } finally {
      setLoading(false);
    }
  };

  const renderStep1 = () => (
    <div className="mx-auto w-full max-w-sm lg:w-96 py-8 lg:py-10">
      <div className="mb-6 text-center lg:text-left">
        <h2 className="text-3xl font-extrabold text-text-main-light dark:text-white tracking-tight mb-2">
          Quên mật khẩu?
        </h2>
        <p className="text-sm text-text-muted-light dark:text-text-muted-dark">
          Nhập email của bạn để nhận mã xác thực đặt lại mật khẩu.
        </p>
      </div>

      <form onSubmit={handleRequestOtp} className="space-y-4">
        <div>
          <label className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5">
            Địa chỉ Email
          </label>
          <div className="relative group">
            <input
              type="email"
              className="appearance-none rounded-xl relative block w-full pl-3 pr-10 py-3 border border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white bg-transparent focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent sm:text-sm transition-all duration-200 hover:border-gray-400 dark:hover:border-gray-500"
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
              required
            />
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
              <Mail className="h-5 w-5 text-gray-400 group-hover:text-primary transition-colors duration-200" />
            </div>
          </div>
        </div>

        <div>
          <button
            type="submit"
            disabled={loading}
            className={`group relative w-full flex justify-center py-3.5 px-4 border border-transparent text-sm font-bold rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-all duration-300 shadow-lg shadow-blue-500/30 hover:shadow-blue-500/50 hover:-translate-y-0.5 bg-gradient-primary ${loading ? "opacity-70 cursor-not-allowed" : "hover:opacity-90"}`}
          >
            {loading ? "Đang gửi..." : "Gửi mã xác thực"}
          </button>
        </div>
      </form>

      <div className="mt-6 text-center">
        <Link
          to="/login"
          className="font-medium text-sm text-primary hover:text-primary-dark transition-colors inline-flex items-center gap-1"
        >
          <ArrowLeft className="w-4 h-4" /> Quay lại đăng nhập
        </Link>
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div className="mx-auto w-full max-w-sm lg:w-96 py-8 lg:py-10">
      <div className="mb-6 text-center lg:text-left">
        <h2 className="text-3xl font-extrabold text-text-main-light dark:text-white tracking-tight mb-2">
          Xác thực OTP
        </h2>
        <p className="text-sm text-text-muted-light dark:text-text-muted-dark">
          Nhập mã 6 số đã được gửi đến <strong>{email}</strong>
        </p>
      </div>

      <form onSubmit={handleVerifyOtp} className="space-y-4">
        <div className="flex justify-center mb-6">
          <OtpInput length={6} onComplete={(code) => setOtp(code)} />
        </div>

        <div>
          <button
            type="submit"
            disabled={loading}
            className={`group relative w-full flex justify-center py-3.5 px-4 border border-transparent text-sm font-bold rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-all duration-300 shadow-lg shadow-blue-500/30 hover:shadow-blue-500/50 hover:-translate-y-0.5 bg-gradient-primary ${loading ? "opacity-70 cursor-not-allowed" : "hover:opacity-90"}`}
          >
            {loading ? "Đang xác thực..." : "Xác nhận"}
          </button>
        </div>
      </form>

      <div className="mt-6 text-center">
        <div className="flex items-center justify-center gap-4">
          <button
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
            <RotateCcw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
            {countdown > 0 ? `Gửi lại sau (${countdown}s)` : "Gửi lại mã"}
          </button>
        </div>
      </div>
    </div>
  );

  const renderStep3 = () => (
    <div className="mx-auto w-full max-w-sm lg:w-96 py-8 lg:py-10">
      <div className="mb-6 text-center lg:text-left">
        <h2 className="text-3xl font-extrabold text-text-main-light dark:text-white tracking-tight mb-2">
          Đặt lại mật khẩu
        </h2>
        <p className="text-sm text-text-muted-light dark:text-text-muted-dark">
          Nhập mật khẩu mới cho tài khoản của bạn.
        </p>
      </div>

      <form onSubmit={handleResetPassword} className="space-y-4">
        <div>
          <label className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5">
            Mật khẩu mới (Tối thiểu 8 ký tự, 1 chữ hoa, 1 số)
          </label>
          <div className="relative group">
            <input
              type="password"
              className="appearance-none rounded-xl relative block w-full pl-3 pr-10 py-3 border border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white bg-transparent focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent sm:text-sm transition-all duration-200 hover:border-gray-400 dark:hover:border-gray-500"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
              required
            />
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
              <Lock className="h-5 w-5 text-gray-400 group-hover:text-primary transition-colors duration-200" />
            </div>
          </div>
          {/* Password Strength Indicator */}
          <div className="mt-3 text-xs space-y-1.5 text-text-muted-light dark:text-text-muted-dark grid grid-cols-2 gap-x-2">
            <p
              className={
                password.length >= 8
                  ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                  : "flex items-center gap-1.5"
              }
            >
              {password.length >= 8 ? (
                <CheckCircle className="w-3.5 h-3.5" />
              ) : (
                <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
              )}
              Tối thiểu 8 ký tự
            </p>
            <p
              className={
                /[A-Z]/.test(password)
                  ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                  : "flex items-center gap-1.5"
              }
            >
              {/[A-Z]/.test(password) ? (
                <CheckCircle className="w-3.5 h-3.5" />
              ) : (
                <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
              )}
              Ít nhất 1 chữ hoa
            </p>
            <p
              className={
                /[a-z]/.test(password)
                  ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                  : "flex items-center gap-1.5"
              }
            >
              {/[a-z]/.test(password) ? (
                <CheckCircle className="w-3.5 h-3.5" />
              ) : (
                <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
              )}
              Ít nhất 1 chữ thường
            </p>
            <p
              className={
                /\d/.test(password)
                  ? "text-green-600 dark:text-green-400 flex items-center gap-1.5"
                  : "flex items-center gap-1.5"
              }
            >
              {/\d/.test(password) ? (
                <CheckCircle className="w-3.5 h-3.5" />
              ) : (
                <span className="w-3.5 h-3.5 block rounded-full bg-gray-300 dark:bg-gray-600" />
              )}
              Ít nhất 1 số
            </p>
          </div>
        </div>

        <div>
          <label className="block text-sm font-semibold text-text-main-light dark:text-white mb-1.5">
            Xác nhận mật khẩu
          </label>
          <div className="relative group">
            <input
              type="password"
              className="appearance-none rounded-xl relative block w-full pl-3 pr-10 py-3 border border-gray-300 dark:border-gray-600 placeholder-gray-400 dark:placeholder-gray-500 text-text-main-light dark:text-white bg-transparent focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent sm:text-sm transition-all duration-200 hover:border-gray-400 dark:hover:border-gray-500"
              placeholder="••••••••"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              disabled={loading}
              required
            />
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
              <CheckCircle
                className={`h-5 w-5 transition-colors ${confirmPassword && password === confirmPassword ? "text-green-500" : "text-gray-400 group-hover:text-primary"}`}
              />
            </div>
          </div>
        </div>

        <div>
          <button
            type="submit"
            disabled={loading}
            className={`group relative w-full flex justify-center py-3.5 px-4 border border-transparent text-sm font-bold rounded-xl text-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-all duration-300 shadow-lg shadow-blue-500/30 hover:shadow-blue-500/50 hover:-translate-y-0.5 bg-gradient-primary ${loading ? "opacity-70 cursor-not-allowed" : "hover:opacity-90"}`}
          >
            {loading ? "Đang xử lý..." : "Đặt lại mật khẩu"}
          </button>
        </div>
      </form>
    </div>
  );

  return (
    <div className="flex min-h-screen bg-background-light dark:bg-background-dark">
      <div className="flex-1 flex flex-col justify-center px-4 sm:px-6 lg:px-20 xl:px-24 overflow-y-auto w-full lg:w-1/2">
        <div className="flex-1 flex flex-col relative z-10 bg-white dark:bg-surface-dark transition-colors duration-300 lg:flex-none">
          <div className="flex-1 flex flex-col justify-center px-4 sm:px-6 lg:px-20 xl:px-24 overflow-y-auto">
            {step === 1 && renderStep1()}
            {step === 2 && renderStep2()}
            {step === 3 && renderStep3()}

            <div className="mt-6 pt-4 border-t border-gray-100 dark:border-gray-800 text-xs text-center text-text-muted-light dark:text-text-muted-dark">
              <p>© 2026 Vivu. All rights reserved.</p>
            </div>
          </div>
        </div>
      </div>

      {/* Right Side Image (Hidden on mobile) */}
      <div className="hidden lg:block relative w-0 flex-1">
        <div className="absolute inset-0 bg-gradient-to-br from-primary via-secondary to-accent opacity-90 z-10 mix-blend-multiply"></div>
        <img
          className="absolute inset-0 h-full w-full object-cover"
          src="https://images.unsplash.com/photo-1506744038136-46273834b3fb?ixlib=rb-1.2.1&auto=format&fit=crop&w=1950&q=80"
          alt="Background"
        />
        <div className="absolute inset-0 z-20 flex flex-col justify-center px-12 text-white">
          <h2 className="text-4xl font-bold mb-6">Khôi phục quyền truy cập</h2>
          <p className="text-lg text-gray-100 max-w-md">
            Đừng lo lắng, chúng tôi sẽ giúp bạn lấy lại mật khẩu và tiếp tục
            khám phá thế giới cùng Vivu.
          </p>
        </div>
      </div>
    </div>
  );
};

export default ForgotPassword;
