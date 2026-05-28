import React, { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, MapPin, Star, Calendar, Clock, User, Globe, Phone } from "lucide-react";
import PropTypes from 'prop-types';

const TABS = [
  { id: "overview", label: "Tổng quan" },
  { id: "activities", label: "Hoạt động" },
  { id: "reviews", label: "Đánh giá" },
];

const LocationLeftDrawer = ({ isOpen, onClose, data }) => {
  const [activeTab, setActiveTab] = useState("overview");

  if (!data) return null;

  // Mock data filling if missing
  const images = data.images || [
    data.img || "/images/ha_long_bay.jpg",
    "https://ui-avatars.com/api/?background=random&name=Img+2",
    "https://ui-avatars.com/api/?background=random&name=Img+3",
    "https://ui-avatars.com/api/?background=random&name=Img+4",
  ];

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            key="drawer-backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 bg-black/20 z-40 cursor-pointer lg:hidden" // Only show backdrop on mobile/tablet where it might cover everything
          />

          {/* Drawer Panel - Left Side */}
          <motion.div
            key="drawer-panel"
            initial={{ x: "-100%" }}
            animate={{ x: 0 }}
            exit={{ x: "-100%" }}
            transition={{ type: "spring", damping: 25, stiffness: 200 }}
            className="fixed inset-y-0 left-0 w-full lg:w-1/2 bg-white shadow-2xl z-50 overflow-hidden flex flex-col border-r border-slate-200"
          >
            {/* Header */}
            <div className="flex items-center justify-between p-6 border-b border-gray-100 sticky top-0 bg-white z-20">
              <div className="flex-1 pr-4">
                <h2 className="text-2xl font-bold text-slate-900">{data.name}</h2>
                <div className="flex items-center text-sm text-slate-500 mt-1">
                  <MapPin className="w-4 h-4 mr-1 text-blue-500" />
                  {data.province}
                </div>
              </div>
              <button
                onClick={onClose}
                className="p-2 rounded-full hover:bg-slate-100 transition-colors shrink-0"
                aria-label="Close"
              >
                <X className="w-6 h-6 text-slate-500" />
              </button>
            </div>

            {/* Scrollable Content */}
            <div className="flex-1 overflow-y-auto scroll-smooth" id="drawer-scroll-container">
              
              {/* Image Gallery */}
              <div className="p-6 pb-0">
                <div className="grid grid-cols-4 grid-rows-2 gap-2 h-[250px] rounded-2xl overflow-hidden">
                  {/* Large Image (Left) */}
                  <div className="col-span-2 row-span-2 relative">
                    <img 
                      src={images[0]} 
                      alt="Main" 
                      className="w-full h-full object-cover hover:scale-105 transition-transform duration-500 cursor-pointer"
                    />
                  </div>
                  {/* Small Images (Right) */}
                  {images.slice(1, 5).map((img, idx) => (
                    <div key={idx} className="relative overflow-hidden bg-slate-100">
                       <img 
                        src={img} 
                        alt={`Collage ${idx}`} 
                        className="w-full h-full object-cover hover:scale-110 transition-transform duration-300 cursor-pointer"
                      />
                    </div>
                  ))}
                </div>
              </div>

              {/* Tabs Navigation */}
              <div className="sticky top-0 bg-white/95 backdrop-blur z-10 px-6 pt-6 border-b border-gray-100 flex space-x-6">
                {TABS.map((tab) => (
                  <button
                    key={tab.id}
                    onClick={() => {
                      const element = document.getElementById(tab.id);
                      if (element) {
                          element.scrollIntoView({ behavior: 'smooth' });
                          setActiveTab(tab.id);
                      }
                    }}
                    className={`pb-3 text-sm font-semibold whitespace-nowrap border-b-2 transition-all ${
                      activeTab === tab.id
                        ? "border-blue-500 text-blue-600"
                        : "border-transparent text-slate-500 hover:text-slate-800"
                    }`}
                  >
                    {tab.label}
                  </button>
                ))}
              </div>

              {/* Content Sections */}
              <div className="p-6 space-y-10 pb-20">
                
                {/* Overview Section */}
                <div id="overview" className="space-y-6 scroll-mt-32">
                     <section>
                          <h3 className="text-lg font-bold text-slate-900 mb-2">Giới thiệu</h3>
                          <p className="text-slate-600 leading-relaxed text-sm">
                            {data.description || "Một điểm đến tuyệt vời với phong cảnh thiên nhiên hùng vĩ và văn hóa độc đáo."}
                          </p>
                     </section>
                     
                    <section>
                        <h3 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
                            Thông tin chi tiết
                        </h3>
                        <div className="bg-slate-50 rounded-2xl border border-slate-100 p-4 space-y-4">
                            <div className="flex items-center gap-3">
                                <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-600 shrink-0">
                                    <Clock className="w-4 h-4" />
                                </div>
                                <div className="text-sm">
                                    <p className="font-semibold text-slate-900">08:00 - 22:00</p>
                                    <p className="text-slate-500 text-xs">Giờ mở cửa</p>
                                </div>
                            </div>
                            <div className="flex items-center gap-3">
                                <div className="w-8 h-8 rounded-full bg-green-100 flex items-center justify-center text-green-600 shrink-0">
                                    <Globe className="w-4 h-4" />
                                </div>
                                <div className="text-sm">
                                    <a href="#" className="font-semibold text-blue-600 hover:underline">www.vivu-vietnam.com</a>
                                    <p className="text-slate-500 text-xs">Website</p>
                                </div>
                            </div>
                        </div>
                    </section>
                </div>

                {/* Reviews Section */}
                <div id="reviews" className="scroll-mt-32">
                    <h3 className="text-lg font-bold text-slate-900 mb-4">Đánh giá nổi bật</h3>
                    <div className="space-y-4">
                        {[1, 2, 3].map((i) => (
                            <div key={i} className="p-4 rounded-xl bg-slate-50 border border-slate-100">
                                <div className="flex items-center gap-2 mb-2">
                                    <div className="w-8 h-8 rounded-full bg-linear-to-r from-blue-400 to-cyan-400 flex items-center justify-center text-xs font-bold text-white">
                                        U{i}
                                    </div>
                                    <div>
                                        <div className="text-sm font-bold text-slate-900">Người dùng {i}</div>
                                        <div className="flex text-amber-400">
                                            {[...Array(5)].map((_, si) => <Star key={si} className="w-3 h-3 fill-current" />)}
                                        </div>
                                    </div>
                                </div>
                                <p className="text-sm text-slate-600">Địa điểm rất đẹp, mình sẽ quay lại!</p>
                            </div>
                        ))}
                    </div>
                </div>

              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
};

LocationLeftDrawer.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  data: PropTypes.object,
};

export default LocationLeftDrawer;
