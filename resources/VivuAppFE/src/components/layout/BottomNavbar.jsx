import React from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { MessageSquare, Briefcase, Compass, Heart, Zap } from "lucide-react";

/**
 * Bottom navigation bar for mobile devices
 * Shows 5 main navigation items in a fixed bottom bar
 */
const BottomNavbar = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [isVisible, setIsVisible] = React.useState(true);
  const lastScrollY = React.useRef(0);

  React.useEffect(() => {
    const handleScroll = () => {
      const currentScrollY = window.scrollY;

      // Handle iOS rubber band effect
      if (currentScrollY < 0) return;

      // Hide when scrolling down past 50px, show when scrolling up
      if (currentScrollY > lastScrollY.current && currentScrollY > 50) {
        if (isVisible) setIsVisible(false);
      } else if (currentScrollY < lastScrollY.current) {
        if (!isVisible) setIsVisible(true);
      }

      lastScrollY.current = currentScrollY;
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [isVisible]);

  const navItems = [
    {
      icon: MessageSquare,
      label: "Chat",
      path: "/chat",
      matchPaths: ["/chat", "/trips"],
    },
    {
      icon: Briefcase,
      label: "Chuyến đi",
      path: "/my-trips",
      matchPaths: ["/my-trips"],
    },
    {
      icon: Compass,
      label: "Khám phá",
      path: "/explore",
      matchPaths: ["/explore"],
    },
    { icon: Heart, label: "Đã lưu", path: "/saved", matchPaths: ["/saved"] },
    {
      icon: Zap,
      label: "Trải nghiệm",
      path: "/inspiration",
      matchPaths: ["/inspiration"],
    },
  ];

  const isActive = (matchPaths) => {
    return matchPaths.some((p) => location.pathname.startsWith(p));
  };

  return (
    <nav 
      className={`lg:hidden fixed bottom-0 left-0 right-0 z-50 bg-white/95 backdrop-blur-md border-t border-gray-100 shadow-lg safe-area-pb transition-transform duration-300 ease-in-out ${
        isVisible ? "translate-y-0" : "translate-y-full"
      }`}
    >
      <div className="flex items-center justify-around h-16 px-2">
        {navItems.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.matchPaths);
          return (
            <button
              key={item.path}
              onClick={() => navigate(item.path)}
              className={`flex flex-col items-center justify-center flex-1 h-full transition-all ${
                active ? "text-blue-600" : "text-gray-400"
              }`}
            >
              <div
                className={`p-1.5 rounded-xl transition-all ${
                  active ? "bg-blue-50" : ""
                }`}
              >
                <Icon size={22} strokeWidth={active ? 2.5 : 2} />
              </div>
              <span
                className={`text-[10px] mt-0.5 font-medium ${active ? "text-blue-600" : "text-gray-500"}`}
              >
                {item.label}
              </span>
            </button>
          );
        })}
      </div>
    </nav>
  );
};

export default BottomNavbar;
