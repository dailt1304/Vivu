import React from "react";
import { createPortal } from "react-dom";
import { motion, AnimatePresence } from "framer-motion";

/**
 * Bottom Sheet for notifications on mobile
 * Using React Portal to avoid stacking context issues with AppNavbar
 * Vercel Rule: patterns-children-over-render-props (using composition)
 */
const NotificationBottomSheet = ({ isOpen, onClose, children }) => {
  // Define the sheet content
  const sheetContent = (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop overlay - High z-index to cover AppNavbar (z-11000) and BottomNavbar (z-50) */}
          <motion.div
            className="fixed inset-0 bg-black/40 backdrop-blur-sm z-[12000] md:hidden"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
          />
          
          {/* Bottom Sheet - Higher z-index than backdrop */}
          <motion.div
            className="fixed bottom-0 left-0 right-0 bg-white rounded-t-2xl shadow-xl z-[12001] md:hidden flex flex-col pt-2 max-h-[85vh] overflow-hidden"
            initial={{ y: "100%" }}
            animate={{ y: 0 }}
            exit={{ y: "100%" }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            drag="y"
            dragConstraints={{ top: 0, bottom: 0 }}
            dragElastic={0.2}
            onDragEnd={(e, info) => {
              // Swipe down threshold to close
              if (info.offset.y > 100 || info.velocity.y > 500) {
                onClose();
              }
            }}
          >
            {/* Drag Handle */}
            <div className="w-full flex justify-center pb-2 cursor-grab active:cursor-grabbing">
              <div className="w-12 h-1.5 bg-gray-300 rounded-full" />
            </div>

            {/* Content Container with independent scrolling */}
            <div className="flex-1 overflow-y-auto w-full pb-8">
               {children}
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );

  // Render via Portal to break out of the Navbar's stacking context
  if (typeof document !== "undefined") {
    return createPortal(sheetContent, document.body);
  }

  return null;
};

export default NotificationBottomSheet;
