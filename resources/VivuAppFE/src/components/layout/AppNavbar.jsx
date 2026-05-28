import React, { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import useClickOutside from "../../hooks/utils/useClickOutside";
import tripApi from "../../api/tripApi";
import { formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";
// eslint-disable-next-line no-unused-vars
import { motion, AnimatePresence } from "framer-motion";
import { useAuth } from "../../contexts/auth-context";
import { useUser } from "../../hooks/useUsers";
import { useUserUsage } from "../../hooks/users/useUsers";
import {
  MessageSquare,
  Briefcase,
  Search,
  Heart,
  Bell,
  Zap,
  LogOut,
  Settings,
  User,
  Plus,
  Compass,
  Crown,
} from "lucide-react";

import NotificationBell from "../notifications/NotificationBell";
import logoImg from "../../assets/images/vivu_logo-remove-background.com.png";

const ChevronDownIcon = ({ className }) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    width="24"
    height="24"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
  >
    <path d="m6 9 6 6 6-6" />
  </svg>
);

const AppNavbar = () => {
  const navigate = useNavigate();
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef(null);

  // Use the unified auth hook
  const { userId, user: authUser, logout, isLoading: isAuthLoading } = useAuth();

  // Use useUser to fetch detailed profile including fullName, avatarUrl
  const { data: fullUser, isLoading: isUserLoading } = useUser(userId);

  // Combine data: prefer full profile data from API, fallback to auth state
  const user = fullUser || authUser;

  // AI Usage - only load if logged in (in a real app, the hook might need conditional key to prevent 401, but here it's fine since useUserUsage will handle token/error or it just returns 401 and stops there)
  const { usage } = useUserUsage();
  
  // Decide if we should show a skeleton:
  // Only show skeleton if we are loading AND we don't have any avatar yet
  const shouldShowAvatarSkeleton = 
    (isAuthLoading || (userId && isUserLoading)) && 
    !user?.avatarUrl && !user?.avatar;

  const handleLogout = () => {
    logout();
  };

  // Chat Dropdown
  const [chatDropdownOpen, setChatDropdownOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [hoveredTab, setHoveredTab] = useState(null);
  const [recentTrips, setRecentTrips] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const chatDropdownRef = useRef(null);
  const scrollContainerRef = useRef(null);

  // Fetch recent trips
  useEffect(() => {
    const fetchRecentTrips = async () => {
      try {
        setIsLoading(true);
        if (!user) return;
        const userId = user.id || user.userId;

        if (userId) {
          const response = await tripApi.getAllByUser(userId);
          if (response.success && response.data?.trips?.items) {
            // Take top 5 recent trips
            const trips = response.data.trips.items.slice(0, 5).map((trip) => {
              const timeString = trip.updatedAt || trip.createdAt;
              return {
                id: trip.id,
                title: trip.title || "Chuyến đi không tên",
                time: timeString
                  ? formatDistanceToNow(new Date(timeString), {
                      addSuffix: true,
                      locale: vi,
                    })
                  : "Vừa xong",
                image:
                  trip.coverUrl ||
                  "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?ixlib=rb-4.0.3&auto=format&fit=crop&w=300&q=80",
              };
            });
            setRecentTrips(trips);
          }
        }
      } catch (error) {
        console.error("Failed to fetch recent trips:", error);
      } finally {
        setIsLoading(false);
      }
    };

    if (chatDropdownOpen && user) {
      fetchRecentTrips();
    }
  }, [chatDropdownOpen, user]);

  // Handle horizontal scroll on wheel
  useEffect(() => {
    const container = scrollContainerRef.current;
    if (!container) return;

    const handleWheel = (e) => {
      // Allow vertical scroll if deltaY is 0 (horizontal only event)
      if (Math.abs(e.deltaY) === 0) return;

      // Stop the page from scrolling vertically
      e.preventDefault();
      e.stopPropagation();

      // Scroll horizontally instead
      container.scrollLeft += e.deltaY;
    };

    // passive: false is required to use preventDefault
    container.addEventListener("wheel", handleWheel, { passive: false });

    return () => {
      container.removeEventListener("wheel", handleWheel);
    };
  }, [chatDropdownOpen]);

  // Close dropdowns when clicking outside
  useClickOutside(dropdownRef, () => setDropdownOpen(false));
  useClickOutside(chatDropdownRef, () => setChatDropdownOpen(false));

  return (
    <nav className="h-14 lg:h-16 border-b border-blue-100 bg-white/80 backdrop-blur-md sticky top-0 z-11000 px-4 lg:px-6 flex items-center justify-between shadow-sm">
      {/* Left: Logo */}
      <div
        className="flex items-center gap-2 cursor-pointer"
        onClick={() => navigate("/chat")}
      >
        <img
          src={logoImg}
          alt="Vivu Logo"
          className="h-8 lg:h-10 w-auto object-contain"
        />
      </div>

      {/* Center: Navigation - Hidden on mobile */}
      <div
        className="hidden lg:flex items-center gap-2 bg-slate-50 border border-slate-100/60 p-1 rounded-full shadow-inner shadow-slate-200/50"
        onMouseLeave={() => setHoveredTab(null)}
      >
        {/* Chat Dropdown */}
        <div className="relative" ref={chatDropdownRef}>
          <button
            onMouseEnter={() => setHoveredTab("chat")}
            onClick={() => setChatDropdownOpen(!chatDropdownOpen)}
            className={`relative flex items-center gap-2 px-5 py-2.5 rounded-full text-sm font-bold transition-colors duration-300 z-10 ${
              window.location.pathname.startsWith("/chat") ||
              (window.location.pathname.startsWith("/trips") &&
                !window.location.pathname.includes("/trips/public")) ||
              chatDropdownOpen
                ? "text-white"
                : "text-slate-500 hover:text-blue-600"
            }`}
          >
            {(window.location.pathname.startsWith("/chat") ||
              (window.location.pathname.startsWith("/trips") &&
                !window.location.pathname.includes("/trips/public")) ||
              chatDropdownOpen) && (
              <motion.div
                layoutId="nav-active"
                className="absolute inset-0 bg-gradient-primary rounded-full shadow-md shadow-blue-200/50 -z-10"
                transition={{ type: "spring", bounce: 0.15, duration: 0.5 }}
              />
            )}

            {!(
              window.location.pathname.startsWith("/chat") ||
              (window.location.pathname.startsWith("/trips") &&
                !window.location.pathname.includes("/trips/public")) ||
              chatDropdownOpen
            ) &&
              hoveredTab === "chat" && (
                <motion.div
                  layoutId="nav-hover"
                  className="absolute inset-0 bg-blue-50/80 rounded-full -z-10"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  transition={{ duration: 0.2 }}
                />
              )}

            <div className="relative z-10 flex items-center gap-2">
              <MessageSquare className="w-4 h-4" />
              <span>Chat</span>
            </div>
          </button>

          {chatDropdownOpen && (
            <div className="absolute top-full left-0 mt-2 w-[540px] bg-white rounded-3xl shadow-2xl border border-slate-100 ring-1 ring-black/5 overflow-hidden z-50 animate-in fade-in zoom-in-95 duration-200 origin-top-left">
              {/* Header: Title & Search */}
              <div className="px-5 py-4 border-b border-slate-50">
                <div className="flex items-center justify-between mb-3">
                  <span className="block text-base font-bold text-slate-800">
                    Chuyến đi của bạn
                  </span>
                  <button
                    onClick={() => {
                      navigate("/my-trips");
                      setChatDropdownOpen(false);
                    }}
                    className="text-xs font-bold text-blue-600 hover:bg-blue-50 px-2 py-1 rounded-lg transition-colors"
                  >
                    Xem tất cả
                  </button>
                </div>

                {/* Search Bar */}
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 w-4 h-4" />
                  <input
                    type="text"
                    placeholder="Tìm kiếm chuyến đi..."
                    className="w-full pl-9 pr-4 py-2 bg-slate-50 border-none rounded-xl text-sm font-medium text-slate-700 placeholder:text-slate-400 focus:ring-2 focus:ring-blue-100 focus:bg-white transition-all outline-none"
                    value={searchQuery || ""}
                    onChange={(e) => setSearchQuery(e.target.value)}
                  />
                </div>
              </div>

              {/* Horizontal Scroll Section */}
              <div className="px-5 py-4">
                <div
                  ref={scrollContainerRef}
                  className="flex gap-3 overflow-x-auto pb-2 hover:pb-2 snap-x overscroll-contain [&::-webkit-scrollbar]:h-1.5 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-slate-200 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-slate-300 transition-colors"
                >
                  {/* New Trip Card - Refined/Delicate */}
                  <button
                    className="snap-start min-w-[120px] w-[120px] h-[120px] flex flex-col items-center justify-center gap-3 rounded-2xl border border-slate-200 bg-white hover:border-blue-400 hover:shadow-md transition-all group text-center shrink-0"
                    onClick={() => {
                      navigate("/chat");
                      setChatDropdownOpen(false);
                    }}
                  >
                    <div className="w-8 h-8 rounded-full border border-slate-300 text-slate-400 flex items-center justify-center group-hover:border-blue-500 group-hover:text-blue-500 group-hover:bg-blue-50 transition-all duration-300">
                      <Plus size={16} strokeWidth={1.5} />
                    </div>
                    <span className="text-xs font-medium text-slate-500 group-hover:text-blue-600 tracking-wide">
                      Tạo mới
                    </span>
                  </button>

                  {/* Recent Trips List - Filtered */}
                  {isLoading
                    ? Array(5)
                        .fill(0)
                        .map((_, i) => (
                          <div
                            key={`skeleton-${i}`}
                            className="snap-start min-w-[120px] w-[120px] h-[120px] rounded-2xl bg-slate-100 animate-pulse shrink-0 border border-slate-200"
                          />
                        ))
                    : recentTrips
                        .filter(
                          (t) =>
                            !searchQuery ||
                            t.title
                              .toLowerCase()
                              .includes(searchQuery.toLowerCase()),
                        )
                        .map((trip) => (
                          <button
                            key={trip.id}
                            className="snap-start min-w-[120px] w-[120px] h-[120px] relative rounded-2xl overflow-hidden group shrink-0 shadow-sm hover:shadow-md transition-all ring-1 ring-slate-100"
                            onClick={() => {
                              navigate(`/trips/${trip.id}`);
                              setChatDropdownOpen(false);
                            }}
                          >
                            {/* Background Image */}
                            <div className="absolute inset-0">
                              <img
                                src={trip.image}
                                alt=""
                                className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
                              />
                              <div className="absolute inset-0 bg-linear-to-t from-black/80 via-transparent to-transparent opacity-80 transition-opacity"></div>
                            </div>

                            {/* Content Overlay */}
                            <div className="absolute inset-x-0 bottom-0 p-2.5 text-left">
                              <h4 className="text-xs font-bold text-white leading-tight mb-0.5 line-clamp-2 drop-shadow-sm">
                                {trip.title}
                              </h4>
                              <p className="text-[9px] text-white/80 font-medium truncate capitalize">
                                {trip.time}
                              </p>
                            </div>
                          </button>
                        ))}
                </div>
              </div>
            </div>
          )}
        </div>

        <NavButton
          id="trips"
          icon={<Briefcase />}
          label="Chuyến đi"
          onClick={() => navigate("/my-trips")}
          hoveredTab={hoveredTab}
          setHoveredTab={setHoveredTab}
          active={
            !chatDropdownOpen &&
            window.location.pathname.startsWith("/my-trips")
          }
        />
        <NavButton
          id="explore"
          icon={<Compass />}
          label="Khám phá"
          onClick={() => navigate("/explore")}
          hoveredTab={hoveredTab}
          setHoveredTab={setHoveredTab}
          active={
            !chatDropdownOpen && window.location.pathname.startsWith("/explore")
          }
        />
        <NavButton
          id="saved"
          icon={<Heart />}
          label="Đã lưu"
          onClick={() => navigate("/saved")}
          hoveredTab={hoveredTab}
          setHoveredTab={setHoveredTab}
          active={
            !chatDropdownOpen && window.location.pathname.startsWith("/saved")
          }
        />

        <NavButton
          id="inspiration"
          icon={<Zap />}
          label="Trải nghiệm"
          onClick={() => navigate("/inspiration")}
          hoveredTab={hoveredTab}
          setHoveredTab={setHoveredTab}
          active={
            !chatDropdownOpen &&
            (window.location.pathname.startsWith("/inspiration") ||
              window.location.pathname.includes("/trips/public"))
          }
        />
      </div>

      {/* Right: Profile */}
      <div className="flex items-center gap-4">
        {/* Usage Badge */}
        {user && usage && usage.limit > 0 && (
          <div 
            onClick={() => navigate("/profile")}
            className={`hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-full border cursor-pointer hover:shadow-sm transition-all ${usage.remaining === 0 ? 'bg-red-50 border-red-100 text-red-600 hover:bg-red-100' : usage.remaining <= 1 ? 'bg-yellow-50 border-yellow-100 text-yellow-600 hover:bg-yellow-100' : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'}`}
            title="Số lượt AI còn lại hôm nay"
          >
            <Zap className={`w-3.5 h-3.5 ${usage.remaining === 0 ? 'fill-red-500 text-red-500' : usage.remaining <= 1 ? 'fill-yellow-500 text-yellow-500' : 'fill-blue-500 text-blue-500'}`} />
            <span className="text-sm font-bold leading-none">{usage.remaining === 0 ? "Hết" : usage.remaining}</span>
          </div>
        )}

        {/* Upgrade / Subscription Button */}
        <button
          onClick={() => navigate("/subscription")}
          className="hidden md:flex items-center gap-2 px-6 py-2 bg-gradient-to-r from-yellow-200 via-yellow-400 to-yellow-500 text-[#451a03] rounded-full text-[14px] font-bold shadow-sm hover:shadow-md hover:-translate-y-0.5 transition-all cursor-pointer ring-1 ring-yellow-300/50 group"
        >
          <Crown
            size={16}
            className="text-[#451a03] fill-[#451a03] group-hover:scale-110 transition-transform"
          />
          <span>Nâng cấp</span>
        </button>

        {/* Notification Bell + Dropdown */}
        <NotificationBell />

        {/* Profile Dropdown */}
        <div className="relative" ref={dropdownRef}>
          <div
            className="flex items-center gap-3 p-1 hover:bg-blue-50/50 rounded-full pr-3 cursor-pointer transition-colors border border-transparent hover:border-blue-100 group"
            onClick={() => setDropdownOpen(!dropdownOpen)}
          >
            <div className="w-9 h-9 rounded-full bg-gradient-primary flex items-center justify-center text-white font-bold text-sm shadow-md ring-2 ring-white overflow-hidden">
              {shouldShowAvatarSkeleton ? (
                <div className="w-full h-full bg-slate-200/60 animate-pulse" />
              ) : user?.avatarUrl || user?.avatar ? (
                <img
                  src={user?.avatarUrl || user?.avatar}
                  alt={user?.fullName || user?.name || "User"}
                  className="w-full h-full object-cover"
                  referrerPolicy="no-referrer"
                />
              ) : (
                <span>
                  {(user?.fullName || user?.name || user?.email || "V")
                    .charAt(0)
                    .toUpperCase()}
                </span>
              )}
            </div>
            <ChevronDownIcon
              className={`w-4 h-4 text-gray-400 group-hover:text-blue-500 transition-transform duration-200 ${dropdownOpen ? "rotate-180" : ""}`}
            />
          </div>

          {/* Dropdown Menu */}
          {dropdownOpen && (
            <div className="absolute right-0 top-full mt-2 w-56 bg-white rounded-xl shadow-xl border border-gray-100 py-2 z-11000 transform origin-top-right transition-all">
              <div className="px-4 py-3 border-b border-gray-50 bg-gray-50/50">
                <p className="text-sm font-bold text-gray-900 truncate">
                  {user?.fullName || user?.name || "Khách"}
                </p>
                <p className="text-xs text-gray-500 truncate">
                  {user?.email || "Chưa đăng nhập"}
                </p>
              </div>

              <div className="p-1">
                <button
                  onClick={() => {
                    navigate("/profile");
                    setDropdownOpen(false);
                  }}
                  className="w-full flex items-center gap-3 px-3 py-2 text-sm text-gray-600 hover:bg-blue-50 hover:text-blue-600 rounded-lg transition-colors"
                >
                  <Settings size={16} />
                  Thông tin cá nhân
                </button>
              </div>

              <div className="border-t border-gray-50 p-1 mt-1">
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-3 px-3 py-2 text-sm text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                >
                  <LogOut size={16} />
                  Đăng xuất
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </nav>
  );
};

const NavButton = ({
  id,
  icon,
  label,
  count,
  active,
  onClick,
  hoveredTab,
  setHoveredTab,
}) => (
  <button
    onClick={onClick}
    onMouseEnter={() => setHoveredTab(id)}
    className={`relative flex items-center gap-2 px-5 py-2.5 rounded-full text-sm font-bold transition-colors duration-300 z-10 ${
      active ? "text-white" : "text-slate-500 hover:text-blue-600"
    }`}
  >
    {active && (
      <motion.div
        layoutId="nav-active"
        className="absolute inset-0 bg-gradient-primary rounded-full shadow-md shadow-blue-200/50 -z-10"
        transition={{ type: "spring", bounce: 0.15, duration: 0.5 }}
      />
    )}
    {!active && hoveredTab === id && (
      <motion.div
        layoutId="nav-hover"
        className="absolute inset-0 bg-blue-50/80 rounded-full -z-10"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.2 }}
      />
    )}
    <div className="relative z-10 flex items-center gap-2">
      {React.cloneElement(icon, { className: "w-4 h-4" })}
      <span>{label}</span>
    </div>
    {count && !active && (
      <span className="absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white ring-2 ring-white z-20">
        {count}
      </span>
    )}
  </button>
);

export default AppNavbar;
