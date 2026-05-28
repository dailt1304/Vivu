import React, { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  Shield,
  Monitor,
  Smartphone,
  AlertTriangle,
  Loader2,
  Check,
  X,
} from "lucide-react";
import SettingsSection from "./SettingsSection";
import SettingsToggle from "./SettingsToggle";
import authApi from "../../../api/authApi";
import toast from "../../../utils/toast";

/**
 * Security settings tab
 */
const SecurityTab = ({
  user,
  onLogoutAll,
  onDeleteAccount,
}) => {
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    newPassword: "",
    confirmPassword: "",
  });
  const [passwordStep, setPasswordStep] = useState(1); // 1: Send OTP, 2: Verify, 3: New Password
  const [otp, setOtp] = useState("");
  const [twoFAEnabled, setTwoFAEnabled] = useState(false);
  const [loading, setLoading] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Mock sessions
  const [sessions] = useState([
    {
      id: "1",
      device: "Windows - Chrome",
      location: "Hà Nội",
      current: true,
      icon: Monitor,
    },
    {
      id: "2",
      device: "iPhone 15 Pro - Safari",
      location: "Đà Nẵng",
      lastActive: "2 ngày trước",
      icon: Smartphone,
    },
  ]);

  // Password validation
  const passwordRequirements = [
    { label: "Ít nhất 8 ký tự", valid: passwordForm.newPassword.length >= 8 },
    { label: "Chữ hoa", valid: /[A-Z]/.test(passwordForm.newPassword) },
    { label: "Chữ thường", valid: /[a-z]/.test(passwordForm.newPassword) },
    { label: "Số", valid: /[0-9]/.test(passwordForm.newPassword) },
  ];

  const isPasswordValid = passwordRequirements.every((r) => r.valid);
  const passwordsMatch =
    passwordForm.newPassword === passwordForm.confirmPassword &&
    passwordForm.confirmPassword.length > 0;

  const handleSendOTP = async () => {
    setLoading(true);
    try {
      const response = await authApi.forgotPassword({ email: user?.email });
      if (response.success) {
        toast.success("Mã xác thực đã được gửi về email của bạn");
        setPasswordStep(2);
      } else {
        toast.error(response.message || "Gửi mã thất bại");
      }
    } catch (error) {
      console.error("Failed to send OTP:", error);
      toast.error("Không thể gửi mã xác thực");
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyOTP = async (e) => {
    e.preventDefault();
    if (!otp) return;
    setLoading(true);
    try {
      const response = await authApi.verifyResetOtp({
        email: user?.email,
        code: otp,
      });
      if (response.success) {
        toast.success("Xác thực thành công");
        setPasswordStep(3);
      } else {
        toast.error(response.message || "Mã xác thực không hợp lệ");
      }
    } catch (error) {
      console.error("Failed to verify OTP:", error);
      toast.error("Xác thực thất bại");
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e) => {
    e.preventDefault();
    if (!isPasswordValid || !passwordsMatch) return;

    setLoading(true);
    try {
      const response = await authApi.resetPassword({
        email: user?.email,
        newPassword: passwordForm.newPassword,
      });

      if (response.success) {
        toast.success("Mật khẩu đã được thay đổi thành công!");
        setPasswordForm({
          newPassword: "",
          confirmPassword: "",
        });
        setOtp("");
        setPasswordStep(1);
      } else {
        toast.error(response.message || "Đổi mật khẩu thất bại");
      }
    } catch (error) {
      console.error("Failed to reset password:", error);
      toast.error("Không thể đổi mật khẩu");
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteAccount = async () => {
    try {
      await onDeleteAccount?.();
    } catch (error) {
      console.error("Failed to delete account:", error);
    }
  };

  return (
    <div className="space-y-6">
      {/* Email Display */}
      <SettingsSection title="Email đăng nhập">
        <div className="flex items-center gap-4 p-4 bg-slate-50/50 border border-slate-100 rounded-[1.25rem] shadow-sm">
          <div className="p-2.5 bg-white rounded-xl shadow-sm ring-1 ring-slate-200/50">
            <Mail size={20} className="text-slate-500" />
          </div>
          <div>
            <p className="font-semibold text-slate-900">
              {user?.handle || "user@email.com"}
            </p>
            <p className="text-sm text-slate-500 mt-0.5">Email không thể thay đổi</p>
          </div>
        </div>
      </SettingsSection>

      {/* Change Password */}
      <SettingsSection
        title="Đổi mật khẩu"
        description="Đảm bảo tài khoản của bạn được bảo mật"
      >
        <div className="space-y-4">
          {/* Step 1: Send OTP */}
          {passwordStep === 1 && (
            <div className="flex flex-col gap-4">
              <p className="text-sm text-slate-600">
                Chúng tôi sẽ gửi một mã xác thực đến email:{" "}
                <span className="font-semibold text-slate-900">{user?.email}</span>
              </p>
              <button
                onClick={handleSendOTP}
                disabled={loading}
                className="w-full sm:w-auto px-8 py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 transition-all flex items-center justify-center gap-2"
              >
                {loading && <Loader2 size={16} className="animate-spin" />}
                Gửi mã xác thực
              </button>
            </div>
          )}

          {/* Step 2: Verify OTP */}
          {passwordStep === 2 && (
            <form onSubmit={handleVerifyOTP} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Mã xác thực (OTP)
                </label>
                <input
                  type="text"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value)}
                  placeholder="Nhập mã 6 chữ số"
                  maxLength={6}
                  className="w-full px-4 py-3 bg-slate-50/50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all text-slate-900 text-center text-xl tracking-[0.5em] font-mono"
                  required
                />
              </div>
              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={loading || otp.length < 6}
                  className="flex-1 px-8 py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 transition-all flex items-center justify-center gap-2"
                >
                  {loading && <Loader2 size={16} className="animate-spin" />}
                  Xác nhận mã
                </button>
                <button
                  type="button"
                  onClick={() => setPasswordStep(1)}
                  className="px-6 py-2.5 text-slate-600 font-semibold hover:bg-slate-100 rounded-xl transition-all"
                >
                  Hủy
                </button>
              </div>
            </form>
          )}

          {/* Step 3: New Password */}
          {passwordStep === 3 && (
            <form onSubmit={handleResetPassword} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Mật khẩu mới
                </label>
                <div className="relative">
                  <input
                    type={showNewPassword ? "text" : "password"}
                    value={passwordForm.newPassword}
                    onChange={(e) =>
                      setPasswordForm((prev) => ({
                        ...prev,
                        newPassword: e.target.value,
                      }))
                    }
                    placeholder="Nhập mật khẩu mới"
                    className="w-full px-4 pr-12 py-3 bg-slate-50/50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all text-slate-900 placeholder:text-slate-400"
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  >
                    {showNewPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>

                {/* Password Requirements */}
                {passwordForm.newPassword && (
                  <div className="mt-2 grid grid-cols-2 gap-2">
                    {passwordRequirements.map((req, i) => (
                      <div
                        key={i}
                        className={`flex items-center gap-1.5 text-xs ${
                          req.valid ? "text-green-600" : "text-gray-400"
                        }`}
                      >
                        {req.valid ? <Check size={12} /> : <X size={12} />}
                        {req.label}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Xác nhận mật khẩu mới
                </label>
                <div className="relative">
                  <input
                    type={showConfirmPassword ? "text" : "password"}
                    value={passwordForm.confirmPassword}
                    onChange={(e) =>
                      setPasswordForm((prev) => ({
                        ...prev,
                        confirmPassword: e.target.value,
                      }))
                    }
                    placeholder="Nhập lại mật khẩu mới"
                    className={`w-full px-4 pr-12 py-3 border bg-slate-50/50 text-slate-900 rounded-xl focus:bg-white focus:outline-none focus:ring-4 transition-all ${
                      passwordForm.confirmPassword
                        ? passwordsMatch
                          ? "border-green-300 focus:ring-green-500/10 focus:border-green-500"
                          : "border-rose-300 focus:ring-rose-500/10 focus:border-rose-500"
                        : "border-slate-200 focus:ring-blue-500/10 focus:border-blue-500"
                    }`}
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  >
                    {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>
              </div>

              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={loading || !isPasswordValid || !passwordsMatch}
                  className="flex-1 px-8 py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 transition-all flex items-center justify-center gap-2"
                >
                  {loading && <Loader2 size={16} className="animate-spin" />}
                  Đổi mật khẩu
                </button>
                <button
                  type="button"
                  onClick={() => setPasswordStep(1)}
                  className="px-6 py-2.5 text-slate-600 font-semibold hover:bg-slate-100 rounded-xl transition-all"
                >
                  Hủy
                </button>
              </div>
            </form>
          )}
        </div>
      </SettingsSection>

    </div>
  );
};

export default SecurityTab;
