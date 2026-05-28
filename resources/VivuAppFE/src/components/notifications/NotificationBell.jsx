import React, { useState, useRef, useEffect } from "react";
import { Bell } from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import { useNotifications } from "../../contexts/notification-context";
import NotificationDropdown, { NotificationListContent } from "./NotificationDropdown";
import NotificationBottomSheet from "./NotificationBottomSheet";

/**
 * Responsive Notification Bell wrapper
 * Handles Bell icon, unread count badge, and toggling Dropdown/BottomSheet
 */
const NotificationBell = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const containerRef = useRef(null);
  const { unreadCount } = useNotifications();
  const [animateBell, setAnimateBell] = useState(false);
  const prevUnreadCount = useRef(unreadCount);

  // Trigger animation when unreadCount increases
  useEffect(() => {
    if (unreadCount > prevUnreadCount.current) {
      setAnimateBell(true);
      // Reset animation state after it completes
      const timer = setTimeout(() => setAnimateBell(false), 500);
      return () => clearTimeout(timer);
    }
    prevUnreadCount.current = unreadCount;
  }, [unreadCount]);

  // Responsive check
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768);
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  // Click outside to close (Desktop)
  useEffect(() => {
    if (isMobile) return;
    const handleClickOutside = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) {
        setIsOpen(false);
      }
    };
    if (isOpen) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen, isMobile]);

  // Body Scroll Lock for mobile
  useEffect(() => {
    if (isMobile && isOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "unset";
    }
    return () => {
      document.body.style.overflow = "unset";
    };
  }, [isOpen, isMobile]);

  const toggleOpen = () => setIsOpen((prev) => !prev);
  const close = () => setIsOpen(false);

  return (
    <div className="relative" ref={containerRef}>
      {/* Bell Button */}
      <button
        onClick={toggleOpen}
        className="p-2 flex items-center justify-center text-gray-500 hover:text-blue-600 bg-white hover:bg-gray-50 rounded-full transition-colors relative outline-none"
        aria-label="Notifications"
      >
        <motion.div
           animate={animateBell ? {
             rotate: [0, -15, 15, -15, 15, 0],
             scale: [1, 1.1, 1]
           } : { rotate: 0, scale: 1 }}
           transition={{ duration: 0.5, ease: "easeInOut" }}
        >
          <Bell size={22} className={isOpen ? "text-blue-600" : ""} />
        </motion.div>
        
        <AnimatePresence>
          {unreadCount > 0 && (
            <motion.div
              initial={{ scale: 0 }}
              animate={{ 
                scale: 1,
                boxShadow: animateBell ? [
                  "0 0 0 0px rgba(239, 68, 68, 0.4)",
                  "0 0 0 10px rgba(239, 68, 68, 0)",
                ] : "0 0 0 0px rgba(239, 68, 68, 0)"
              }}
              transition={{
                scale: { type: "spring", stiffness: 400, damping: 25 },
                boxShadow: { duration: 0.6, repeat: animateBell ? 1 : 0 }
              }}
              exit={{ scale: 0 }}
              className="absolute top-1 right-1.5 min-w-[18px] h-[18px] bg-red-500 rounded-full border-2 border-white flex items-center justify-center pointer-events-none z-10"
            >
              <span className="text-[10px] font-bold text-white px-1 leading-none">
                {unreadCount > 9 ? "9+" : unreadCount}
              </span>
            </motion.div>
          )}
        </AnimatePresence>
      </button>

      {/* Responsive Panels */}
      <AnimatePresence>
        {!isMobile && isOpen ? (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95 }}
            transition={{ duration: 0.15 }}
            className="absolute right-0 top-full mt-2 w-[360px] md:w-[400px] bg-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.12)] border border-gray-100/50 z-50 overflow-hidden transform origin-top-right"
          >
            <div className="h-[480px]">
               <NotificationListContent onClose={close} />
            </div>
          </motion.div>
        ) : null}
      </AnimatePresence>

      {isMobile && (
        <NotificationBottomSheet isOpen={isOpen} onClose={close}>
          <div className="h-[75vh] min-h-[400px]">
             <NotificationListContent onClose={close} />
          </div>
        </NotificationBottomSheet>
      )}
    </div>
  );
};

export default NotificationBell;
