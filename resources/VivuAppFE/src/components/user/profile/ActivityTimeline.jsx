import React from "react";
import { motion } from "framer-motion";
import { MapPin, Plane, BookOpen, Star, Heart, Clock } from "lucide-react";
import PropTypes from "prop-types";

/**
 * Activity timeline for profile page
 */
const ActivityTimeline = ({ activities = [] }) => {
  const getActivityIcon = (type) => {
    const icons = {
      trip: { icon: Plane, color: "bg-blue-500", ring: "ring-blue-100" },
      blog: { icon: BookOpen, color: "bg-purple-500", ring: "ring-purple-100" },
      review: { icon: Star, color: "bg-amber-500", ring: "ring-amber-100" },
      save: { icon: Heart, color: "bg-red-500", ring: "ring-red-100" },
      visit: { icon: MapPin, color: "bg-green-500", ring: "ring-green-100" },
    };
    return icons[type] || icons.visit;
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffDays = Math.floor((now - date) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return "Hôm nay";
    if (diffDays === 1) return "Hôm qua";
    if (diffDays < 7) return `${diffDays} ngày trước`;

    return date.toLocaleDateString("vi-VN", {
      day: "2-digit",
      month: "short",
      year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
    });
  };

  // Group activities by month
  const groupedActivities = activities.reduce((groups, activity) => {
    const date = new Date(activity.date);
    const monthKey = `${date.getFullYear()}-${date.getMonth()}`;
    const monthLabel = date.toLocaleDateString("vi-VN", {
      month: "long",
      year: "numeric",
    });

    if (!groups[monthKey]) {
      groups[monthKey] = { label: monthLabel, items: [] };
    }
    groups[monthKey].items.push(activity);
    return groups;
  }, {});

  if (activities.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400">
        <Clock size={48} className="mb-4 opacity-50" />
        <p className="font-medium">Chưa có hoạt động nào</p>
        <p className="text-sm">Bắt đầu khám phá để tạo dấu ấn của bạn!</p>
      </div>
    );
  }

  return (
    <div className="relative">
      {Object.entries(groupedActivities).map(
        ([monthKey, group], groupIndex) => (
          <div key={monthKey} className="mb-8">
            {/* Month Header */}
            <div className="flex items-center gap-3 mb-4">
              <div className="h-px flex-1 bg-gray-200" />
              <span className="text-sm font-bold text-gray-500 uppercase tracking-wide">
                {group.label}
              </span>
              <div className="h-px flex-1 bg-gray-200" />
            </div>

            {/* Activities */}
            <div className="relative">
              {/* Timeline line */}
              <div className="absolute left-5 top-0 bottom-0 w-0.5 bg-gray-200" />

              {group.items.map((activity, index) => {
                const {
                  icon: Icon,
                  color,
                  ring,
                } = getActivityIcon(activity.type);

                return (
                  <motion.div
                    key={activity.id}
                    initial={{ opacity: 0, x: -20 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: index * 0.1 }}
                    className="relative flex gap-4 pb-6 last:pb-0"
                  >
                    {/* Icon */}
                    <div
                      className={`relative z-10 w-10 h-10 rounded-full ${color} ${ring} ring-4 flex items-center justify-center text-white shadow-lg`}
                    >
                      <Icon size={18} />
                    </div>

                    {/* Content */}
                    <div className="flex-1 bg-white rounded-xl border border-gray-100 p-4 shadow-sm hover:shadow-md transition-shadow">
                      <div className="flex items-start justify-between mb-1">
                        <h4 className="font-semibold text-gray-900">
                          {activity.title}
                        </h4>
                        <span className="text-xs text-gray-400">
                          {formatDate(activity.date)}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600">
                        {activity.description}
                      </p>

                      {/* Image preview if available */}
                      {activity.image && (
                        <div className="mt-3 rounded-lg overflow-hidden">
                          <img
                            src={activity.image}
                            alt={activity.title}
                            className="w-full h-32 object-cover"
                          />
                        </div>
                      )}

                      {/* Stats if available */}
                      {activity.stats && (
                        <div className="flex items-center gap-4 mt-3 pt-3 border-t border-gray-100 text-xs text-gray-500">
                          {activity.stats.views && (
                            <span>{activity.stats.views} lượt xem</span>
                          )}
                          {activity.stats.likes && (
                            <span>❤️ {activity.stats.likes}</span>
                          )}
                          {activity.stats.rating && (
                            <span>⭐ {activity.stats.rating}</span>
                          )}
                        </div>
                      )}
                    </div>
                  </motion.div>
                );
              })}
            </div>
          </div>
        ),
      )}
    </div>
  );
};

ActivityTimeline.propTypes = {
  activities: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.string.isRequired,
      type: PropTypes.oneOf(["trip", "blog", "review", "save", "visit"])
        .isRequired,
      title: PropTypes.string.isRequired,
      description: PropTypes.string,
      date: PropTypes.string.isRequired,
      image: PropTypes.string,
      stats: PropTypes.object,
    }),
  ),
};

export default ActivityTimeline;
