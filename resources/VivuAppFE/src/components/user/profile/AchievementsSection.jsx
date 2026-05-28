import React, { useState } from "react";
import { motion } from "framer-motion";
import { Trophy, ChevronRight } from "lucide-react";
import PropTypes from "prop-types";
import AchievementBadge from "./AchievementBadge";
import { ACHIEVEMENTS, getAchievementProgress } from "../../data/achievements";

/**
 * Achievements section for profile page
 */
const AchievementsSection = ({ userStats = {} }) => {
  const [showAll, setShowAll] = useState(false);

  // Calculate which achievements are unlocked
  const achievementsWithStatus = ACHIEVEMENTS.map((achievement) => ({
    ...achievement,
    unlocked: achievement.condition(userStats),
    progress: getAchievementProgress(achievement, userStats),
  }));

  // Sort: unlocked first, then by progress
  const sortedAchievements = [...achievementsWithStatus].sort((a, b) => {
    if (a.unlocked && !b.unlocked) return -1;
    if (!a.unlocked && b.unlocked) return 1;
    return b.progress.percentage - a.progress.percentage;
  });

  const displayedAchievements = showAll
    ? sortedAchievements
    : sortedAchievements.slice(0, 6);

  const unlockedCount = achievementsWithStatus.filter((a) => a.unlocked).length;

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-gradient-to-br from-amber-400 to-orange-500 rounded-xl text-white">
            <Trophy size={20} />
          </div>
          <div>
            <h3 className="font-bold text-gray-900">Thành tích</h3>
            <p className="text-xs text-gray-500">
              {unlockedCount}/{ACHIEVEMENTS.length} đã mở khóa
            </p>
          </div>
        </div>
        <button
          onClick={() => setShowAll(!showAll)}
          className="flex items-center gap-1 text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
        >
          {showAll ? "Thu gọn" : "Xem tất cả"}
          <ChevronRight
            size={16}
            className={`transition-transform ${showAll ? "rotate-90" : ""}`}
          />
        </button>
      </div>

      {/* Achievements Grid */}
      <div className="p-6">
        <motion.div
          layout
          className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-6"
        >
          {displayedAchievements.map((achievement, index) => (
            <motion.div
              key={achievement.id}
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: index * 0.05 }}
              className="relative"
            >
              <AchievementBadge
                icon={achievement.icon}
                title={achievement.title}
                description={achievement.description}
                unlocked={achievement.unlocked}
                progress={!achievement.unlocked ? achievement.progress : null}
                size="md"
              />
            </motion.div>
          ))}
        </motion.div>

        {/* Progress Summary */}
        <div className="mt-6 pt-4 border-t border-gray-100">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium text-gray-600">
              Tiến độ tổng thể
            </span>
            <span className="text-sm font-bold text-gray-900">
              {Math.round((unlockedCount / ACHIEVEMENTS.length) * 100)}%
            </span>
          </div>
          <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
            <motion.div
              initial={{ width: 0 }}
              animate={{
                width: `${(unlockedCount / ACHIEVEMENTS.length) * 100}%`,
              }}
              transition={{ duration: 0.8, ease: "easeOut" }}
              className="h-full bg-gradient-to-r from-amber-400 to-orange-500 rounded-full"
            />
          </div>
        </div>
      </div>
    </div>
  );
};

AchievementsSection.propTypes = {
  userStats: PropTypes.object,
};

export default AchievementsSection;
