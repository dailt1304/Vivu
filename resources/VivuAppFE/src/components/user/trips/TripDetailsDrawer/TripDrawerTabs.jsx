import React from "react";
import { motion } from "framer-motion";
import { TRIP_TABS } from "./constants";

/**
 * TripDrawerTabs - Tab navigation for TripDetailsDrawer
 *
 * Extracted from TripDetailsDrawer to reduce component size
 * and enable independent testing/styling.
 */
function TripDrawerTabs({ activeTab, onTabChange }) {
  return (
    <div className="sticky top-0 z-30 bg-white/85 backdrop-blur-2xl border-b border-gray-100/50 p-2 md:p-3">
      <div className="flex p-1 bg-gray-100/80 rounded-2xl w-full overflow-x-auto no-scrollbar scrollbar-hide shadow-inner relative ring-1 ring-black/5">
        {TRIP_TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => onTabChange(tab.id)}
            className={`flex-1 shrink-0 flex items-center justify-center gap-1.5 md:gap-2 py-2 md:py-2.5 px-4 rounded-xl text-[13px] md:text-sm font-bold relative z-10 transition-colors duration-300 ${
              activeTab === tab.id
                ? "text-blue-700"
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            <tab.icon
              size={15}
              strokeWidth={activeTab === tab.id ? 2.5 : 2}
              className={
                activeTab === tab.id ? "text-blue-600" : "text-gray-400"
              }
            />
            <span className="tracking-tight whitespace-nowrap">
              {tab.label}
            </span>
            {activeTab === tab.id && (
              <motion.div
                layoutId="drawerTabBackground"
                className="absolute inset-0 bg-white rounded-xl shadow-[0_2px_12px_rgba(0,0,0,0.08)] ring-1 ring-black/5 -z-10"
                transition={{ type: "spring", bounce: 0.15, duration: 0.5 }}
              />
            )}
          </button>
        ))}
      </div>
    </div>
  );
}

export default React.memo(TripDrawerTabs);
