import React from "react";
import { motion } from "framer-motion";
import { User, Shield, CreditCard, LogOut, MapPinPlus, Flag } from "lucide-react";
import PropTypes from "prop-types";

const menuItems = [
  { id: "personal", label: "Thông tin cá nhân", icon: User },
  { id: "security", label: "Bảo mật", icon: Shield },
  { id: "payment", label: "Thanh toán", icon: CreditCard },
  { id: "submissions", label: "Địa điểm đã gửi", icon: MapPinPlus },
  { id: "reports", label: "Địa điểm đã báo cáo", icon: Flag },
];

/**
 * Settings sidebar navigation
 */
const SettingsSidebar = ({ activeTab, onTabChange, onLogout }) => {
  return (
    <div className="w-full lg:w-64 shrink-0 font-medium">
      <nav className="flex flex-col gap-1.5 p-2 bg-transparent">
        <div className="space-y-1">
          {menuItems.map((item) => {
            const isActive = activeTab === item.id;
            const Icon = item.icon;

            return (
              <button
                key={item.id}
                onClick={() => onTabChange(item.id)}
                className={`relative z-0 w-full flex items-center gap-3 px-4 py-3 rounded-2xl text-left transition-colors font-semibold ${
                  isActive
                    ? "text-slate-900"
                    : "text-slate-500 hover:text-slate-900 hover:bg-slate-200/50"
                }`}
              >
                {isActive && (
                  <motion.div
                    layoutId="sidebar-indicator"
                    className="absolute inset-0 z-[-1] bg-white rounded-2xl shadow-sm border border-slate-200/80"
                    transition={{ type: "spring", stiffness: 200, damping: 25 }}
                  />
                )}
                <Icon size={18} className={`relative z-10 ${isActive ? "text-blue-600" : ""}`} />
                <span className="relative z-10 tracking-wide">{item.label}</span>
              </button>
            );
          })}
        </div>

        {/* Divider */}
        <div className="mx-4 my-2 h-px bg-slate-200/50" />

        {/* Logout */}
        <div className="p-2 mt-auto">
          <button
            onClick={onLogout}
            className="w-full flex items-center gap-3 px-4 py-3 rounded-2xl text-left text-rose-600 hover:bg-rose-50/50 transition-colors font-semibold"
          >
            <LogOut size={18} />
            <span className="tracking-wide">Đăng xuất</span>
          </button>
        </div>
      </nav>
    </div>
  );
};

SettingsSidebar.propTypes = {
  activeTab: PropTypes.string.isRequired,
  onTabChange: PropTypes.func.isRequired,
  onLogout: PropTypes.func.isRequired,
};

export default SettingsSidebar;
