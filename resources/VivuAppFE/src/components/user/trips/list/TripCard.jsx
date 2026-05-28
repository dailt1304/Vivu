import { Calendar, MapPin, Clock, Trash2, Star, Globe } from "lucide-react";
import { motion } from "framer-motion";

// Hoisted
const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 },
};

const TripCard = ({
  id,
  image,
  title,
  dates,
  location,
  status,
  days,
  isRated,
  rating,
  isPublic,
  onDelete,
  onToggleVisibility,
  onClick,
}) => {
  const getStatusColor = (s) => {
    switch (s) {
      case "planning":
        return "bg-blue-50 text-blue-600 border-blue-100";
      case "upcoming":
        return "bg-orange-50 text-orange-600 border-orange-100";
      case "ongoing":
        return "bg-indigo-50 text-indigo-600 border-indigo-100";
      case "completed":
        return "bg-emerald-50 text-emerald-600 border-emerald-100";
      case "cancelled":
        return "bg-red-50 text-red-600 border-red-100";
      default:
        return "bg-gray-50 text-gray-600 border-gray-100";
    }
  };

  const getStatusText = (s) => {
    switch (s) {
      case "planning":
        return "Đang lên kế hoạch";
      case "upcoming":
        return "Sắp diễn ra";
      case "ongoing":
        return "Đang diễn ra";
      case "completed":
        return "Đã hoàn thành";
      case "cancelled":
        return "Đã hủy";
      default:
        return s;
    }
  };

  return (
    <motion.div
      variants={itemVariants}
      whileHover={{ y: -6, scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
      transition={{ type: "spring", stiffness: 100, damping: 20 }}
      onClick={() => onClick(id)}
      className="group relative h-64 rounded-3xl overflow-hidden shadow-sm hover:shadow-[0_20px_40px_-5px_rgb(0,0,0,0.15)] ring-1 ring-slate-900/5 transition-shadow duration-500 cursor-pointer"
    >
      {/* Background Image */}
      <img
        src={image}
        alt={title}
        className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700 ease-in-out"
      />

      {/* Gradient Overlay */}
      <div className="absolute inset-0 bg-linear-to-t from-black/90 via-black/20 to-transparent opacity-90" />

      {/* Status Badge */}
      <div className="absolute top-4 left-4 z-20">
        <span
          className={`px-3 py-1.5 rounded-full text-xs font-bold border shadow-sm ${getStatusColor(status)}`}
        >
          {getStatusText(status)}
        </span>
      </div>

      {/* Action Buttons */}
      <div className="absolute top-4 right-4 z-20 flex gap-2">
        {status === "completed" && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              onToggleVisibility(id, !isPublic);
            }}
            className={`p-2 backdrop-blur-md rounded-full transition-all shadow-sm opacity-0 group-hover:opacity-100 translate-x-2 group-hover:translate-x-0 duration-300 border ${
              isPublic
                ? "bg-blue-500/20 text-blue-400 border-blue-500/30 hover:bg-blue-500 hover:text-white"
                : "bg-white/20 text-white/70 border-white/20 hover:bg-white/40 hover:text-white"
            }`}
            title={isPublic ? "Đang công khai" : "Đang riêng tư"}
          >
            <Globe className="w-4 h-4" />
          </button>
        )}
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(id);
          }}
          className="p-2 bg-white/20 text-white backdrop-blur-md rounded-full hover:bg-red-500 hover:text-white transition-all shadow-sm opacity-0 group-hover:opacity-100 translate-x-2 group-hover:translate-x-0 duration-300"
          title="Xóa chuyến đi"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>

      {/* Content Overlay */}
      <div className="absolute bottom-0 left-0 right-0 p-6 z-20">
        {/* Meta Row */}
        <div className="flex items-center gap-3 mb-2 text-xs font-semibold text-white/80 uppercase tracking-wider">
          <div className="flex items-center gap-1">
            <MapPin className="w-3.5 h-3.5 text-blue-400" />
            {location}
          </div>
          <span>•</span>
          <div className="flex items-center gap-1">
            <Clock className="w-3.5 h-3.5 text-orange-400" />
            {days} ngày
          </div>
          {isRated ? (
            <>
              <span>•</span>
              <div className="flex items-center gap-1 text-yellow-400">
                <Star className="w-3.5 h-3.5 fill-yellow-400" />
                <span>{rating ? rating : "Đã đánh giá"}</span>
              </div>
            </>
          ) : null}
        </div>

        {/* Title */}
        <h3 className="text-xl font-bold text-white mb-3 line-clamp-2 leading-tight group-hover:text-blue-200 transition-colors">
          {title}
        </h3>

        {/* Date Badge */}
        <div className="flex items-center gap-2 text-sm text-white/90 bg-white/10 p-2.5 rounded-xl backdrop-blur-md border border-white/10 w-fit">
          <Calendar className="w-4 h-4 text-blue-200" />
          <span className="font-medium">{dates}</span>
        </div>
      </div>
    </motion.div>
  );
};

export default TripCard;
