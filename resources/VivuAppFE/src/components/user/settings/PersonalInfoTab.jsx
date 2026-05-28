import React, { useState, useEffect, memo, useMemo } from "react";
import {
  Camera,
  User,
  Loader2,
  FileText,
  UserRound,
  Sparkles,
} from "lucide-react";
import { motion } from "framer-motion";
import SettingsSection from "./SettingsSection";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../../ui/select";

// --- Constants ---
const DAYS = Array.from({ length: 31 }, (_, i) => i + 1);
const MONTHS = Array.from({ length: 12 }, (_, i) => i + 1);
const YEARS = Array.from(
  { length: 100 },
  (_, i) => new Date().getFullYear() - i,
);

const SPRING_CONFIG = { type: "spring", stiffness: 400, damping: 30 };

// --- Sub-components ---

const BirthdayPicker = memo(
  ({ currentDay, currentMonth, currentYear, onChange }) => {
    return (
      <div className="flex flex-col gap-2 md:col-span-2 lg:col-span-1">
        <label className="text-sm font-semibold text-slate-700 ml-1">
          Ngày sinh
        </label>
        <div className="grid grid-cols-3 gap-3">
          {/* Day */}
          <Select
            value={currentDay.replace(/^0/, "")}
            onValueChange={(value) => onChange("day", value)}
          >
            <SelectTrigger className="h-12 bg-slate-50/50 border-slate-200 rounded-2xl focus:ring-blue-500/10 focus:border-blue-500 transition-colors hover:bg-white">
              <SelectValue placeholder="Ngày" />
            </SelectTrigger>
            <SelectContent className="max-h-60 rounded-2xl border-slate-200">
              {DAYS.map((d) => (
                <SelectItem key={d} value={String(d)} className="rounded-xl">
                  {d}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {/* Month */}
          <Select
            value={currentMonth.replace(/^0/, "")}
            onValueChange={(value) => onChange("month", value)}
          >
            <SelectTrigger className="h-12 bg-slate-50/50 border-slate-200 rounded-2xl focus:ring-blue-500/10 focus:border-blue-500 transition-colors hover:bg-white">
              <SelectValue placeholder="Tháng" />
            </SelectTrigger>
            <SelectContent className="max-h-60 rounded-2xl border-slate-200">
              {MONTHS.map((m) => (
                <SelectItem key={m} value={String(m)} className="rounded-xl">
                  Tháng {m}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {/* Year */}
          <Select
            value={currentYear}
            onValueChange={(value) => onChange("year", value)}
          >
            <SelectTrigger className="h-12 bg-slate-50/50 border-slate-200 rounded-2xl focus:ring-blue-500/10 focus:border-blue-500 transition-colors hover:bg-white">
              <SelectValue placeholder="Năm" />
            </SelectTrigger>
            <SelectContent className="max-h-60 rounded-2xl border-slate-200">
              {YEARS.map((y) => (
                <SelectItem key={y} value={String(y)} className="rounded-xl">
                  {y}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
    );
  },
);

const GenderPicker = memo(({ currentGender, onChange }) => {
  const options = [
    { value: "male", label: "Nam", icon: User },
    { value: "female", label: "Nữ", icon: UserRound },
    { value: "other", label: "Khác", icon: Sparkles },
  ];

  return (
    <div className="md:col-span-2 lg:col-span-1">
      <label className="block text-sm font-semibold text-slate-700 mb-2 ml-1">
        Giới tính
      </label>
      <div className="flex gap-3">
        {options.map((option) => {
          const isSelected = currentGender === option.value;
          const Icon = option.icon;

          return (
            <motion.button
              key={option.value}
              type="button"
              whileHover={{ y: -2 }}
              whileTap={{ scale: 0.98 }}
              onClick={() => onChange("gender", option.value)}
              className={`relative flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl border transition-colors duration-300 ${
                isSelected
                  ? "bg-gradient-primary text-white border-transparent shadow-lg shadow-blue-500/25 ring-2 ring-blue-500/10"
                  : "bg-slate-50/50 border-slate-200 text-slate-600 hover:bg-white hover:border-blue-200"
              }`}
            >
              <Icon
                size={18}
                className={
                  isSelected ? "text-white" : "text-slate-400 transition-colors"
                }
              />
              <span className="font-bold text-sm tracking-tight">
                {option.label}
              </span>
              {isSelected && (
                <motion.div
                  layoutId="active-gender"
                  className="absolute inset-0 rounded-2xl bg-white/10"
                  initial={false}
                  transition={SPRING_CONFIG}
                />
              )}
            </motion.button>
          );
        })}
      </div>
    </div>
  );
});

/**
 * Personal information settings tab
 */
const PersonalInfoTab = ({ user, onSave }) => {
  const [formData, setFormData] = useState(() => ({
    fullName: user?.fullName || "",
    phone: user?.phone || "",
    dateOfBirth: user?.dateOfBirth || "",
    gender: user?.gender || "",
    bio: user?.bio || "",
    avatarUrl: user?.avatarUrl || "",
  }));
  const [loading, setLoading] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  // Synchronize form data with user prop when user data loads via SWR
  useEffect(() => {
    if (user) {
      setFormData({
        fullName: user.fullName || "",
        phone: user.phone || "",
        dateOfBirth: user.dateOfBirth || "",
        gender: user.gender || "",
        bio: user.bio || "",
        avatarUrl: user.avatarUrl || "",
      });
      setHasChanges(false);
    }
  }, [user]);

  // Date helper logic
  const birthDate = formData.dateOfBirth
    ? new Date(formData.dateOfBirth)
    : null;
  const currentDay = formData.dateOfBirth
    ? formData.dateOfBirth.split("-")[2]
    : "";
  const currentMonth = formData.dateOfBirth
    ? formData.dateOfBirth.split("-")[1]
    : "";
  const currentYear = formData.dateOfBirth
    ? formData.dateOfBirth.split("-")[0]
    : "";

  const handleDatePartChange = (part, value) => {
    let y = currentYear || "2000";
    let m = currentMonth || "01";
    let d = currentDay || "01";

    if (part === "year") y = value;
    if (part === "month") m = value.padStart(2, "0");
    if (part === "day") d = value.padStart(2, "0");

    const newDate = `${y}-${m}-${d}`;
    handleChange("dateOfBirth", newDate);
  };

  const handleChange = (field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setHasChanges(true);
  };

  const handleAvatarUpload = (e) => {
    const file = e.target.files[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        alert("Ảnh quá lớn. Vui lòng chọn ảnh dưới 5MB.");
        return;
      }
      const reader = new FileReader();
      reader.onloadend = () => {
        handleChange("avatarUrl", reader.result);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await onSave?.(formData);
      setHasChanges(false);
    } catch (error) {
      console.error("Failed to save:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    setFormData({
      fullName: user?.fullName || "",
      phone: user?.phone || "",
      dateOfBirth: user?.dateOfBirth || "",
      gender: user?.gender || "",
      bio: user?.bio || "",
      avatarUrl: user?.avatarUrl || "",
    });
    setHasChanges(false);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Avatar Section */}
      <SettingsSection
        title="Ảnh đại diện"
        description="Hỗ trợ JPG, PNG. Tối đa 5MB"
      >
        <div className="flex items-center gap-6">
          <div className="relative group">
            <div className="w-24 h-24 rounded-full overflow-hidden bg-slate-100 ring-1 ring-slate-200">
              {formData.avatarUrl ? (
                <img
                  src={formData.avatarUrl}
                  alt="Avatar"
                  className="w-full h-full object-cover"
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center bg-slate-50 text-slate-400">
                  <User size={32} />
                </div>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="w-fit px-4 py-2 bg-white ring-1 ring-slate-200 shadow-sm text-slate-700 font-semibold rounded-xl hover:bg-slate-50 active:scale-[0.98] transition-all cursor-pointer flex items-center gap-2">
              <Camera size={18} />
              Thay đổi ảnh
              <input
                type="file"
                accept="image/jpeg,image/png"
                onChange={handleAvatarUpload}
                className="hidden"
              />
            </label>
            {formData.avatarUrl && (
              <button
                type="button"
                onClick={() => handleChange("avatarUrl", "")}
                className="w-fit px-4 py-2 text-rose-600 font-semibold hover:bg-rose-50 rounded-xl transition-all active:scale-[0.98]"
              >
                Xóa ảnh
              </button>
            )}
          </div>
        </div>
      </SettingsSection>

      {/* Basic Info Section */}
      <SettingsSection title="Thông tin cơ bản">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {/* Full Name */}
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-slate-700">
              Họ và tên <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={formData.fullName}
              onChange={(e) => handleChange("fullName", e.target.value)}
              placeholder="Nhập họ và tên"
              required
              minLength={2}
              maxLength={50}
              className="w-full px-4 py-3 bg-slate-50/50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all text-slate-900 placeholder:text-slate-400"
            />
          </div>

          {/* Phone */}
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-slate-700">
              Số điện thoại
            </label>
            <input
              type="tel"
              value={formData.phone}
              onChange={(e) => handleChange("phone", e.target.value)}
              placeholder="0912 345 678"
              pattern="[0-9]{10,11}"
              className="w-full px-4 py-3 bg-slate-50/50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all text-slate-900 placeholder:text-slate-400"
            />
            <p className="text-xs text-slate-400">Định dạng: 10-11 số</p>
          </div>

          {/* Birthday */}
          <BirthdayPicker
            currentDay={currentDay}
            currentMonth={currentMonth}
            currentYear={currentYear}
            onChange={handleDatePartChange}
          />

          {/* Gender */}
          <GenderPicker
            currentGender={formData.gender}
            onChange={handleChange}
          />

          {/* Bio */}
          <div className="md:col-span-2 flex flex-col gap-1.5 mt-2">
            <label className="text-sm font-medium text-slate-700 flex items-center gap-2">
              <FileText size={16} className="text-slate-400" />
              Giới thiệu bản thân
            </label>
            <textarea
              value={formData.bio}
              onChange={(e) => handleChange("bio", e.target.value)}
              placeholder="Chia sẻ một chút về sở thích du lịch của bạn..."
              rows={4}
              maxLength={500}
              className="w-full px-4 py-3 bg-slate-50/50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all text-slate-900 placeholder:text-slate-400 resize-none"
            />
          </div>
        </div>
      </SettingsSection>

      {/* Action Buttons */}
      {hasChanges && (
        <div className="flex justify-end gap-3 pt-4">
          <button
            type="button"
            onClick={handleCancel}
            className="px-5 py-2.5 text-slate-600 font-semibold hover:bg-slate-100 rounded-xl transition-all active:scale-[0.98]"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={loading}
            className="px-8 py-3 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 hover:-translate-y-0.5 active:scale-[0.98] transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {loading && <Loader2 size={16} className="animate-spin" />}
            Lưu thay đổi
          </button>
        </div>
      )}
    </form>
  );
};

export default PersonalInfoTab;
