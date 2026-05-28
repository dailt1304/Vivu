import React, { useState, useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { ArrowLeft, User, Shield, CreditCard, Bell, MapPinPlus, Flag } from "lucide-react";
import AppNavbar from "../../../components/layout/AppNavbar";
import SettingsSidebar from "../../../components/user/settings/SettingsSidebar";
import PersonalInfoTab from "../../../components/user/settings/PersonalInfoTab";
import SecurityTab from "../../../components/user/settings/SecurityTab";
import PaymentTab from "../../../components/user/settings/PaymentTab";
import UserSubmissionsTab from "../../../components/user/profile/UserSubmissionsTab";
import UserReportsTab from "../../../components/user/settings/UserReportsTab";
import { useAuth } from "../../../contexts/auth-context";
import { useUpdateProfile, useUploadAvatar } from "../../../hooks/users/useUsers";
import { useUser } from "../../../hooks/useUsers";
import toast from "../../../utils/toast";

const mobileTabs = [
  { id: "personal", label: "Cá nhân", icon: User },
  { id: "security", label: "Bảo mật", icon: Shield },
  { id: "payment", label: "Thanh toán", icon: CreditCard },
  { id: "submissions", label: "Địa điểm đã gửi", icon: MapPinPlus },
  { id: "reports", label: "Báo cáo", icon: Flag },
];

/**
 * Settings page with 4 tabs
 */
const SettingsPage = () => {
  const navigate = useNavigate();
  const { user: authUser, logout, refreshUser } = useAuth();
  const { updateProfile } = useUpdateProfile();
  const { uploadAvatar } = useUploadAvatar();
  const { data: userData, mutate: mutateUser } = useUser(authUser?.id || authUser?.userId);
  const [activeTab, setActiveTab] = useState("personal");

  // Derived user state from auth hook or SWR data
  // rule: rerender-derived-state-no-effect
  const user = useMemo(
    () => ({
      fullName: userData?.fullName || authUser?.fullName || "Người dùng",
      handle: authUser?.email || "user@email.com",
      avatarUrl: userData?.avatarUrl || authUser?.avatarUrl || "",
      phone: userData?.phone || "",
      dateOfBirth: userData?.dateOfBirth ? userData.dateOfBirth.split("T")[0] : "",
      gender: userData?.gender || "",
      bio: userData?.bio || "",
      email: userData?.email || authUser?.email || "",
    }),
    [userData, authUser],
  );

  const handleLogout = () => {
    logout();
  };

  const handleSavePersonalInfo = async (formData) => {
    try {
      let avatarUrl = undefined;

      // If avatarUrl is a base64 data URI or a File, upload it first
      if (formData.avatarUrl) {
        if (formData.avatarUrl instanceof File) {
          // Direct File object
          const uploadRes = await uploadAvatar(formData.avatarUrl);
          avatarUrl = uploadRes?.data || uploadRes;
        } else if (typeof formData.avatarUrl === "string" && formData.avatarUrl.startsWith("data:")) {
          // Convert base64 data URI to File, then upload
          const res = await fetch(formData.avatarUrl);
          const blob = await res.blob();
          const file = new File([blob], "avatar.png", { type: blob.type });
          const uploadRes = await uploadAvatar(file);
          avatarUrl = uploadRes?.data || uploadRes;
        } else {
          // Already a normal URL, pass as-is
          avatarUrl = formData.avatarUrl;
        }
      }

      const payload = {
        fullName: formData.fullName || undefined,
        bio: formData.bio || undefined,
        phone: formData.phone || undefined,
        gender: formData.gender || undefined,
        dateOfBirth: formData.dateOfBirth || undefined,
        avatarUrl: avatarUrl,
      };

      const response = await updateProfile(payload);

      if (response?.success) {
        toast.success("Cập nhật thông tin thành công");
        // Sync local auth state for Navbar/Profile
        if (response.data) {
          refreshUser(response.data);
          mutateUser(); // Refresh SWR cache
        }
      } else {
        toast.error(response?.message || "Cập nhật thất bại");
      }
    } catch (error) {
      console.error("Failed to save personal info:", error);
      toast.error("Đã xảy ra lỗi khi cập nhật");
    }
  };

  const handleLogoutAll = async () => {
    // In real app, call API here
    console.log("Logout all devices");
    handleLogout();
  };

  const handleDeleteAccount = async () => {
    // In real app, call API here
    console.log("Delete account");
    handleLogout();
  };

  const renderTabContent = () => {
    switch (activeTab) {
      case "personal":
        return <PersonalInfoTab user={user} onSave={handleSavePersonalInfo} />;
      case "security":
        return (
          <SecurityTab
            user={user}
            onLogoutAll={handleLogoutAll}
            onDeleteAccount={handleDeleteAccount}
          />
        );
      case "payment":
        return <PaymentTab />;
      case "submissions":
        return <UserSubmissionsTab />;
      case "reports":
        return <UserReportsTab />;
      default:
        return null;
    }
  };

  const getTabTitle = () => {
    const titles = {
      personal: "Thông tin cá nhân",
      security: "Bảo mật",
      payment: "Thanh toán",
      submissions: "Địa điểm đã gửi",
      reports: "Địa điểm đã báo cáo",
    };
    return titles[activeTab] || "Cài đặt";
  };

  return (
    <div className="flex flex-col min-h-screen bg-slate-50 relative selection:bg-blue-500/30">
      <AppNavbar />

      <div className="flex-1 pb-20 relative z-10">
        {/* Header - Integrated & Transparent */}
        <div className="pt-10 pb-6 px-4 sm:px-6 lg:px-8 max-w-6xl mx-auto">
          <div className="flex items-center gap-4">
            <button
              onClick={() => navigate(-1)}
              className="p-2 text-slate-500 hover:text-slate-900 hover:bg-slate-200/50 rounded-xl transition-all"
            >
              <ArrowLeft size={20} />
            </button>
            <div>
              <h1 className="text-3xl tracking-tight font-black text-slate-900">
                Thông tin cá nhân
              </h1>
              <p className="text-sm font-medium text-slate-500 mt-1">
                Quản lý thông tin và tài khoản của bạn
              </p>
            </div>
          </div>
        </div>

        {/* Mobile Tabs */}
        <div className="lg:hidden bg-slate-50/80 backdrop-blur-xl border-b border-slate-200/50 sticky top-14 z-20">
          <div className="flex overflow-x-auto px-4 py-2">
            {mobileTabs.map((tab) => {
              const Icon = tab.icon;
              const isActive = activeTab === tab.id;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-4 py-2.5 rounded-xl whitespace-nowrap transition-colors ${
                    isActive
                      ? "bg-blue-50 text-blue-600"
                      : "text-gray-500 hover:bg-gray-50"
                  }`}
                >
                  <Icon size={18} />
                  <span className="font-medium text-sm">{tab.label}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Main Content */}
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 mt-2">
          <div className="flex flex-col lg:flex-row gap-10">
            {/* Desktop Sidebar */}
            <div className="hidden lg:block">
              <SettingsSidebar
                activeTab={activeTab}
                onTabChange={setActiveTab}
                onLogout={handleLogout}
              />
            </div>

            {/* Content Area */}
            <div className="flex-1 min-w-0">
              {/* Tab Title (Mobile) */}
              <div className="lg:hidden mb-6">
                <h2 className="text-xl font-bold text-gray-900">
                  {getTabTitle()}
                </h2>
              </div>

              {/* Tab Content */}
              <AnimatePresence mode="wait">
                <motion.div
                  key={activeTab}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -10 }}
                  transition={{ duration: 0.2 }}
                >
                  {renderTabContent()}
                </motion.div>
              </AnimatePresence>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SettingsPage;
