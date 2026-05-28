import React, { useState } from "react";
import {
  Bell,
  Mail,
  MessageCircle,
  Heart,
  UserPlus,
  MapPin,
  Loader2,
} from "lucide-react";
import SettingsSection from "./SettingsSection";
import SettingsToggle from "./SettingsToggle";

/**
 * Notifications settings tab
 */
const NotificationsTab = ({ onSave }) => {
  const [settings, setSettings] = useState({
    // Push
    pushEnabled: true,
    // Email
    emailTripUpdates: true,
    emailPromotions: false,
    emailNews: true,
    emailWeeklyDigest: false,
    // Activity
    activityComments: true,
    activityLikes: true,
    activityFollowers: true,
    activitySuggestions: true,
  });
  const [loading, setLoading] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  const handleChange = (key, value) => {
    setSettings((prev) => ({ ...prev, [key]: value }));
    setHasChanges(true);
  };

  const handleSave = async () => {
    setLoading(true);
    try {
      await onSave?.(settings);
      setHasChanges(false);
    } catch (error) {
      console.error("Failed to save:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    setSettings({
      pushEnabled: true,
      emailTripUpdates: true,
      emailPromotions: false,
      emailNews: true,
      emailWeeklyDigest: false,
      activityComments: true,
      activityLikes: true,
      activityFollowers: true,
      activitySuggestions: true,
    });
    setHasChanges(false);
  };

  return (
    <div className="space-y-6">
      {/* Push Notifications */}
      <SettingsSection title="Push Notification">
        <SettingsToggle
          label="Thông báo đẩy"
          description="Nhận thông báo trên trình duyệt"
          checked={settings.pushEnabled}
          onChange={(v) => handleChange("pushEnabled", v)}
        />
        <div className="flex items-center gap-2 mt-3 p-3 bg-blue-50 rounded-lg text-sm text-blue-700">
          <Bell size={16} />
          <span>
            {settings.pushEnabled
              ? "Bạn sẽ nhận thông báo ngay khi có cập nhật mới"
              : "Bật để không bỏ lỡ thông tin quan trọng"}
          </span>
        </div>
      </SettingsSection>

      {/* Email Notifications */}
      <SettingsSection
        title="Thông báo Email"
        description="Tùy chỉnh email bạn muốn nhận"
      >
        <div className="divide-y divide-gray-100">
          <SettingsToggle
            label="Cập nhật chuyến đi"
            description="Nhắc nhở lịch trình, thay đổi booking"
            checked={settings.emailTripUpdates}
            onChange={(v) => handleChange("emailTripUpdates", v)}
          />
          <SettingsToggle
            label="Khuyến mãi"
            description="Ưu đãi và giảm giá đặc biệt"
            checked={settings.emailPromotions}
            onChange={(v) => handleChange("emailPromotions", v)}
          />
          <SettingsToggle
            label="Tin tức & Nội dung"
            description="Blog mới, tips du lịch"
            checked={settings.emailNews}
            onChange={(v) => handleChange("emailNews", v)}
          />
          <SettingsToggle
            label="Bản tin hàng tuần"
            description="Tổng hợp tin tức mỗi tuần"
            checked={settings.emailWeeklyDigest}
            onChange={(v) => handleChange("emailWeeklyDigest", v)}
          />
        </div>
      </SettingsSection>

      {/* Activity Notifications */}
      <SettingsSection
        title="Hoạt động"
        description="Thông báo về tương tác trên nội dung của bạn"
      >
        <div className="divide-y divide-gray-100">
          <div className="flex items-center gap-3 py-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <MessageCircle size={18} className="text-blue-600" />
            </div>
            <div className="flex-1">
              <SettingsToggle
                label="Bình luận mới"
                description="Khi có người bình luận bài viết của bạn"
                checked={settings.activityComments}
                onChange={(v) => handleChange("activityComments", v)}
              />
            </div>
          </div>

          <div className="flex items-center gap-3 py-3">
            <div className="p-2 bg-red-100 rounded-lg">
              <Heart size={18} className="text-red-500" />
            </div>
            <div className="flex-1">
              <SettingsToggle
                label="Lượt thích"
                description="Khi có người thích bài viết của bạn"
                checked={settings.activityLikes}
                onChange={(v) => handleChange("activityLikes", v)}
              />
            </div>
          </div>

          <div className="flex items-center gap-3 py-3">
            <div className="p-2 bg-green-100 rounded-lg">
              <UserPlus size={18} className="text-green-600" />
            </div>
            <div className="flex-1">
              <SettingsToggle
                label="Người theo dõi mới"
                description="Khi có người mới theo dõi bạn"
                checked={settings.activityFollowers}
                onChange={(v) => handleChange("activityFollowers", v)}
              />
            </div>
          </div>

          <div className="flex items-center gap-3 py-3">
            <div className="p-2 bg-purple-100 rounded-lg">
              <MapPin size={18} className="text-purple-600" />
            </div>
            <div className="flex-1">
              <SettingsToggle
                label="Đề xuất địa điểm"
                description="Gợi ý địa điểm dựa trên sở thích"
                checked={settings.activitySuggestions}
                onChange={(v) => handleChange("activitySuggestions", v)}
              />
            </div>
          </div>
        </div>
      </SettingsSection>

      {/* Action Buttons */}
      {hasChanges && (
        <div className="flex justify-end gap-3 pt-4">
          <button
            type="button"
            onClick={handleCancel}
            className="px-5 py-2.5 text-gray-600 font-medium hover:bg-gray-100 rounded-xl transition-colors"
          >
            Hủy
          </button>
          <button
            onClick={handleSave}
            disabled={loading}
            className="px-5 py-2.5 bg-gradient-to-r from-blue-500 to-indigo-600 text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {loading && <Loader2 size={16} className="animate-spin" />}
            Lưu thay đổi
          </button>
        </div>
      )}
    </div>
  );
};

export default NotificationsTab;
