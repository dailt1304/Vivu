import React, { useState } from "react";
import { motion } from "framer-motion";
import { Map, ChevronRight, MapPin } from "lucide-react";
import PropTypes from "prop-types";

// Simplified Vietnam provinces data (63 provinces)
const VIETNAM_PROVINCES = [
  { code: "HN", name: "Hà Nội", region: "north" },
  { code: "HCM", name: "TP. Hồ Chí Minh", region: "south" },
  { code: "DN", name: "Đà Nẵng", region: "central" },
  { code: "HP", name: "Hải Phòng", region: "north" },
  { code: "CT", name: "Cần Thơ", region: "south" },
  { code: "DL", name: "Đà Lạt", region: "highland" },
  { code: "QN", name: "Quảng Ninh", region: "north" },
  { code: "TH", name: "Thanh Hóa", region: "north" },
  { code: "NA", name: "Nghệ An", region: "north" },
  { code: "HT", name: "Hà Tĩnh", region: "central" },
  { code: "QB", name: "Quảng Bình", region: "central" },
  { code: "QT", name: "Quảng Trị", region: "central" },
  { code: "TTH", name: "Thừa Thiên Huế", region: "central" },
  { code: "QNa", name: "Quảng Nam", region: "central" },
  { code: "QNg", name: "Quảng Ngãi", region: "central" },
  { code: "BD", name: "Bình Định", region: "central" },
  { code: "PY", name: "Phú Yên", region: "central" },
  { code: "KH", name: "Khánh Hòa", region: "central" },
  { code: "NT", name: "Ninh Thuận", region: "central" },
  { code: "BTh", name: "Bình Thuận", region: "central" },
  { code: "GL", name: "Gia Lai", region: "highland" },
  { code: "KT", name: "Kon Tum", region: "highland" },
  { code: "DLa", name: "Đắk Lắk", region: "highland" },
  { code: "DN2", name: "Đắk Nông", region: "highland" },
  { code: "LĐ", name: "Lâm Đồng", region: "highland" },
  { code: "BP", name: "Bình Phước", region: "south" },
  { code: "TN", name: "Tây Ninh", region: "south" },
  { code: "BD2", name: "Bình Dương", region: "south" },
  { code: "ĐN", name: "Đồng Nai", region: "south" },
  { code: "BR", name: "Bà Rịa - Vũng Tàu", region: "south" },
  { code: "LA", name: "Long An", region: "south" },
  { code: "TG", name: "Tiền Giang", region: "south" },
  { code: "BT", name: "Bến Tre", region: "south" },
  { code: "TV", name: "Trà Vinh", region: "south" },
  { code: "VL", name: "Vĩnh Long", region: "south" },
  { code: "ĐT", name: "Đồng Tháp", region: "south" },
  { code: "AG", name: "An Giang", region: "south" },
  { code: "KG", name: "Kiên Giang", region: "south" },
  { code: "HG2", name: "Hậu Giang", region: "south" },
  { code: "ST", name: "Sóc Trăng", region: "south" },
  { code: "BL", name: "Bạc Liêu", region: "south" },
  { code: "CM", name: "Cà Mau", region: "south" },
  // North provinces
  { code: "HG", name: "Hà Giang", region: "north" },
  { code: "CB", name: "Cao Bằng", region: "north" },
  { code: "BK", name: "Bắc Kạn", region: "north" },
  { code: "TQ", name: "Tuyên Quang", region: "north" },
  { code: "LC", name: "Lào Cai", region: "north" },
  { code: "YB", name: "Yên Bái", region: "north" },
  { code: "TN2", name: "Thái Nguyên", region: "north" },
  { code: "LS", name: "Lạng Sơn", region: "north" },
  { code: "BG", name: "Bắc Giang", region: "north" },
  { code: "PT", name: "Phú Thọ", region: "north" },
  { code: "VP", name: "Vĩnh Phúc", region: "north" },
  { code: "BN", name: "Bắc Ninh", region: "north" },
  { code: "HD", name: "Hải Dương", region: "north" },
  { code: "HY", name: "Hưng Yên", region: "north" },
  { code: "TB", name: "Thái Bình", region: "north" },
  { code: "HN2", name: "Hà Nam", region: "north" },
  { code: "NĐ", name: "Nam Định", region: "north" },
  { code: "NB", name: "Ninh Bình", region: "north" },
  { code: "SL", name: "Sơn La", region: "north" },
  { code: "HB", name: "Hòa Bình", region: "north" },
  { code: "ĐB", name: "Điện Biên", region: "north" },
  { code: "LCh", name: "Lai Châu", region: "north" },
];

/**
 * Travel map showing visited provinces
 */
const TravelMap = ({ visitedProvinces = [], onExpand }) => {
  const [hoveredProvince, setHoveredProvince] = useState(null);

  const visitedCount = visitedProvinces.length;
  const totalProvinces = 63;
  const percentage = Math.round((visitedCount / totalProvinces) * 100);

  // Get region color
  const getRegionColor = (region, isVisited) => {
    if (!isVisited) return "bg-gray-100";
    const colors = {
      north: "bg-blue-400",
      central: "bg-green-400",
      highland: "bg-amber-400",
      south: "bg-rose-400",
    };
    return colors[region] || "bg-blue-400";
  };

  // Group provinces by region for display
  const regions = {
    north: VIETNAM_PROVINCES.filter((p) => p.region === "north"),
    central: VIETNAM_PROVINCES.filter((p) => p.region === "central"),
    highland: VIETNAM_PROVINCES.filter((p) => p.region === "highland"),
    south: VIETNAM_PROVINCES.filter((p) => p.region === "south"),
  };

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-gradient-to-br from-green-400 to-emerald-600 rounded-xl text-white">
            <Map size={20} />
          </div>
          <div>
            <h3 className="font-bold text-gray-900">Bản đồ du lịch</h3>
            <p className="text-xs text-gray-500">
              Đã khám phá {visitedCount}/{totalProvinces} tỉnh thành
            </p>
          </div>
        </div>
        <button
          onClick={onExpand}
          className="flex items-center gap-1 text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
        >
          Mở rộng
          <ChevronRight size={16} />
        </button>
      </div>

      {/* Map Visualization */}
      <div className="p-6">
        {/* Progress Bar */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium text-gray-600">
              Tiến độ khám phá
            </span>
            <span className="text-sm font-bold text-gray-900">
              {percentage}%
            </span>
          </div>
          <div className="h-3 bg-gray-100 rounded-full overflow-hidden">
            <motion.div
              initial={{ width: 0 }}
              animate={{ width: `${percentage}%` }}
              transition={{ duration: 1, ease: "easeOut" }}
              className="h-full bg-gradient-to-r from-green-400 via-emerald-500 to-teal-500 rounded-full"
            />
          </div>
        </div>

        {/* Region Grid */}
        <div className="space-y-4">
          {Object.entries(regions).map(([regionName, provinces]) => {
            const regionLabels = {
              north: "Miền Bắc",
              central: "Miền Trung",
              highland: "Tây Nguyên",
              south: "Miền Nam",
            };
            const visitedInRegion = provinces.filter((p) =>
              visitedProvinces.includes(p.code),
            );

            return (
              <div key={regionName}>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                    {regionLabels[regionName]}
                  </span>
                  <span className="text-xs text-gray-400">
                    {visitedInRegion.length}/{provinces.length}
                  </span>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {provinces.map((province) => {
                    const isVisited = visitedProvinces.includes(province.code);
                    return (
                      <motion.div
                        key={province.code}
                        whileHover={{ scale: 1.1 }}
                        onMouseEnter={() => setHoveredProvince(province)}
                        onMouseLeave={() => setHoveredProvince(null)}
                        className={`relative w-8 h-8 rounded-lg flex items-center justify-center text-[10px] font-bold cursor-pointer transition-all ${
                          isVisited
                            ? `${getRegionColor(regionName, true)} text-white shadow-sm`
                            : "bg-gray-100 text-gray-400 hover:bg-gray-200"
                        }`}
                      >
                        {province.code.substring(0, 2)}

                        {/* Tooltip */}
                        {hoveredProvince?.code === province.code && (
                          <div className="absolute -top-10 left-1/2 -translate-x-1/2 z-20">
                            <div className="bg-gray-900 text-white text-xs px-2 py-1 rounded-lg whitespace-nowrap">
                              {province.name}
                              {isVisited && " ✓"}
                            </div>
                          </div>
                        )}
                      </motion.div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        {/* Legend */}
        <div className="mt-6 pt-4 border-t border-gray-100 flex flex-wrap gap-4 text-xs">
          <div className="flex items-center gap-1.5">
            <div className="w-3 h-3 rounded bg-blue-400" />
            <span className="text-gray-600">Miền Bắc</span>
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-3 h-3 rounded bg-green-400" />
            <span className="text-gray-600">Miền Trung</span>
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-3 h-3 rounded bg-amber-400" />
            <span className="text-gray-600">Tây Nguyên</span>
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-3 h-3 rounded bg-rose-400" />
            <span className="text-gray-600">Miền Nam</span>
          </div>
        </div>

        {/* Achievement Badge */}
        {percentage >= 50 && (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="mt-4 p-3 bg-gradient-to-r from-amber-50 to-orange-50 rounded-xl border border-amber-100 flex items-center gap-3"
          >
            <span className="text-2xl">🏆</span>
            <div>
              <p className="font-bold text-amber-800 text-sm">
                Nhà thám hiểm Việt Nam!
              </p>
              <p className="text-xs text-amber-600">
                Bạn đã khám phá hơn 50% lãnh thổ
              </p>
            </div>
          </motion.div>
        )}
      </div>
    </div>
  );
};

TravelMap.propTypes = {
  visitedProvinces: PropTypes.arrayOf(PropTypes.string),
  onExpand: PropTypes.func,
};

export default TravelMap;
