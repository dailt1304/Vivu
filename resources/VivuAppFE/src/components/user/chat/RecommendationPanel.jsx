import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  MapPin,
  Map,
  Compass,
  Briefcase,
  Zap
} from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import Card from './Card';
import TripCard from './TripCard';

const tabs = [
    { id: 'for_you', label: 'Dành cho bạn' },
    { id: 'jump_back', label: 'Quay lại' },
    { id: 'inspiration', label: 'Cảm hứng' },
];

const RecommendationPanel = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState(tabs[0].id);
  const [direction, setDirection] = useState(0);

  const handleTabChange = (newTabId) => {
      const oldIndex = tabs.findIndex(t => t.id === activeTab);
      const newIndex = tabs.findIndex(t => t.id === newTabId);
      setDirection(newIndex > oldIndex ? 1 : -1);
      setActiveTab(newTabId);
  };

  const variants = {
    initial: (direction) => ({
      x: direction > 0 ? 50 : -50,
      opacity: 0
    }),
    animate: {
      x: 0,
      opacity: 1
    },
    exit: (direction) => ({
      x: direction < 0 ? 50 : -50,
      opacity: 0
    })
  };

  return (
    <div className="h-full bg-slate-50/50 flex flex-col">
       {/* Tabs Header - Segmented Control */}
       <div className="px-6 pt-6 pb-2">
            <div className="flex items-center p-1 bg-slate-200/50 rounded-full relative">
                {tabs.map((tab) => (
                        <button
                            key={tab.id}
                            onClick={() => handleTabChange(tab.id)}
                            className={`relative flex-1 py-2 text-xs font-bold rounded-full transition-all z-10 ${
                                activeTab === tab.id 
                                ? 'text-slate-700 shadow-sm' 
                                : 'text-slate-500 hover:text-slate-600'
                            }`}
                        >
                            {activeTab === tab.id && (
                                <motion.div
                                    layoutId="activeTabBg"
                                    className="absolute inset-0 bg-white rounded-full shadow-sm"
                                    transition={{ type: "spring", stiffness: 500, damping: 30 }}
                                />
                            )}
                            <span className="relative z-10">{tab.label}</span>
                        </button>
                ))}
            </div>
       </div>

       {/* Content Area */}
       <div className="flex-1 overflow-y-auto p-6 scrollbar-hide overflow-x-hidden relative">
         <AnimatePresence mode="wait" custom={direction}>
           {activeTab === 'for_you' && (
               <motion.div
                 key="for_you"
                 custom={direction}
                 variants={variants}
                 initial="initial"
                 animate="animate"
                 exit="exit"
                 transition={{ duration: 0.2 }}
               >
                   <div className="flex items-center justify-between mb-6">
                       <div className="flex items-center gap-2">
                           <h2 className="text-xl font-bold text-gray-900">Dành cho bạn tại</h2>
                           <div className="flex items-center gap-1 bg-white border border-gray-200 px-2 py-1 rounded-md text-sm font-semibold">
                               <MapPin className="w-3 h-3" /> Đà Nẵng
                           </div>
                       </div>
                       <button 
                            onClick={() => navigate('/explore')}
                            className="bg-white border border-gray-200 px-3 py-1 rounded-full text-xs font-semibold hover:bg-gray-50 flex items-center gap-1"
                        >
                            <Compass className="w-3 h-3" /> Khám phá
                       </button>
                   </div>

                   <div className="grid grid-cols-2 gap-4">
                       <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Wink Hotel Danang Centre"
                         subtitle="Khách sạn"
                         icon={<Briefcase className="w-3 h-3" />}
                       />
                        <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Nhà hàng Madame Lân"
                         subtitle="Món Việt"
                         icon={<Zap className="w-3 h-3" />}
                       />
                        <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Ngũ Hành Sơn"
                         subtitle="Tham quan"
                         icon={<Map className="w-3 h-3" />}
                       />
                       <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Cầu Rồng"
                         subtitle="Tham quan"
                         icon={<Map className="w-3 h-3" />}
                       />
                   </div>
               </motion.div>
           )}

           {activeTab === 'jump_back' && (
               <motion.div
                 key="jump_back"
                 custom={direction}
                 variants={variants}
                 initial="initial"
                 animate="animate"
                 exit="exit"
                 transition={{ duration: 0.2 }}
               >
                   <div className="flex items-center justify-between mb-6">
                       <h2 className="text-xl font-bold text-gray-900">Quay lại</h2>
                   </div>
                   <div className="grid grid-cols-2 gap-4">
                        <TripCard 
                            image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19"
                            title="Chuyến đi Đà Nẵng"
                            initials={["B", "T", "L"]}
                        />
                         <TripCard 
                            image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19"
                            title="Chuyến đi Ý"
                        />
                         <TripCard 
                            image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19"
                            title="Chuyến đi Huế"
                        />
                   </div>
               </motion.div>
           )}

           {activeTab === 'inspiration' && (
               <motion.div
                 key="inspiration"
                 custom={direction}
                 variants={variants}
                 initial="initial"
                 animate="animate"
                 exit="exit"
                 transition={{ duration: 0.2 }}
               >
                   <div className="flex items-center justify-between mb-6">
                       <h2 className="text-xl font-bold text-gray-900">Nguồn cảm hứng</h2>
                   </div>
                   <div className="grid grid-cols-2 gap-4">
                       <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Lịch trình Đà Nẵng 3 ngày"
                         subtitle="Hướng dẫn"
                         large
                       />
                       <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Cẩm nang Đà Nẵng"
                         subtitle="Hướng dẫn"
                         large
                       />
                       <Card 
                         image="https://encrypted-tbn0.gstatic.com/licensed-image?q=tbn:ANd9GcQuOyegABsFL98nMPhKTUjHCTSfcj-_egzlfsN_2KfoiLxKUbnYlqFJdQOJHl82VKQG6MU3E6rTs_BxCIwUKP8yWgM&s=19" 
                         title="Đà Nẵng: Chuyến đi gia đình"
                         subtitle="Hướng dẫn"
                         large
                       />
                   </div>
               </motion.div>
           )}
         </AnimatePresence>
       </div>
    </div>
  );
};

export default RecommendationPanel;
