import React, { useState, useEffect } from "react";
import { format, differenceInDays } from "date-fns";
import { MapPin, CalendarDays, Users, Wallet, Heart, Calendar } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import tripApi from "../../../api/tripApi";
import { useAuth } from "../../../contexts/auth-context";
import { useNavigate } from "react-router-dom";
import { formatDistanceToNow } from "date-fns";
import { vi } from "date-fns/locale";

const TripPreviewPanel = ({ data }) => {
  const { destination, startDate, endDate, tripSize, budget, interests, title, selectedCoverUrl } = data;
  const [recentTrips, setRecentTrips] = useState([]);
  const [isRecentTripsLoading, setIsRecentTripsLoading] = useState(true);
  const { user, isLoading: isAuthLoading } = useAuth();
  const navigate = useNavigate();

  const destinationName = typeof destination === 'object' && destination !== null ? destination.name : destination;
  
  const duration = (startDate && endDate && startDate <= endDate) 
    ? differenceInDays(endDate, startDate)
    : 0;

  const PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1488646953014-85cb44e25828?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80";

  const destinationImage = selectedCoverUrl || PLACEHOLDER_IMAGE;

  useEffect(() => {
    const fetchRecentTrips = async () => {
      if (isAuthLoading) return; // Wait for auth check to finish
      if (!user) {
        setIsRecentTripsLoading(false);
        return;
      }
      
      try {
        setIsRecentTripsLoading(true);
        const userId = user.id || user.userId;

        if (userId) {
          const response = await tripApi.getAllByUser(userId);
          if (response.success && response.data?.trips?.items) {
            const trips = response.data.trips.items.slice(0, 3).map((trip) => {
              const timeString = trip.updatedAt || trip.createdAt;
              return {
                id: trip.id,
                title: trip.title || "Chuyến đi không tên",
                time: timeString
                  ? formatDistanceToNow(new Date(timeString), {
                      addSuffix: true,
                      locale: vi,
                    })
                  : "Vừa xong",
                image: trip.coverUrl || "https://images.unsplash.com/photo-1476514525535-07fb3b4ae5f1?ixlib=rb-4.0.3&auto=format&fit=crop&w=300&q=80",
              };
            });
            setRecentTrips(trips);
          }
        }
      } catch (error) {
        console.error("Failed to fetch recent trips:", error);
      } finally {
        setIsRecentTripsLoading(false);
      }
    };

    fetchRecentTrips();
  }, [user, isAuthLoading]);

  const budgetMap = { saving: "Tiết kiệm", balanced: "Cân đối", luxury: "Cao cấp" };

  return (
    <div className="h-full w-full bg-slate-50/80 flex flex-col items-center justify-start p-6 lg:p-10 relative overflow-y-auto no-scrollbar">
      
      {/* Background Decor */}
      <div className="absolute top-0 right-0 w-96 h-96 bg-blue-100/40 rounded-full blur-3xl -translate-y-1/2 translate-x-1/3 pointer-events-none"></div>
      <div className="absolute bottom-0 left-0 w-96 h-96 bg-indigo-100/40 rounded-full blur-3xl translate-y-1/3 -translate-x-1/3 pointer-events-none"></div>

      {/* Main Preview Card */}
      <div className="w-full max-w-sm mb-8 z-10 perspective-1000 group">
        <h3 className="text-sm font-bold text-slate-500 uppercase tracking-widest mb-4 ml-1">Live Preview</h3>
        
        <motion.div 
          className="bg-white rounded-[2rem] overflow-hidden shadow-[0_20px_40px_-15px_rgba(0,0,0,0.05)] border border-slate-100 relative group-hover:-translate-y-2 transition-transform duration-500 will-change-transform"
          animate={{ y: [0, -4, 0] }}
          transition={{ repeat: Infinity, duration: 6, ease: "easeInOut" }}
        >
          {/* Cover Image */}
          <div className="h-48 w-full bg-slate-200 relative overflow-hidden">
            <img 
              src={destinationImage} 
              alt="Destination" 
              className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
            />
            <div className="absolute inset-0 bg-linear-to-t from-slate-900/80 via-slate-900/20 to-transparent"></div>
            
            {/* Overlay Info */}
            <div className="absolute bottom-0 left-0 p-5 w-full">
              <h2 className="text-xl font-extrabold text-white line-clamp-1 drop-shadow-md">
                {title || (destinationName ? `Chuyến đi ${destinationName}` : "Chuyến đi mới")}
              </h2>
              {destinationName && (
                <p className="text-white/80 text-sm font-medium flex items-center gap-1 mt-1">
                  <MapPin size={14} /> {destinationName}
                </p>
              )}
            </div>
          </div>

          {/* Details Body */}
          <div className="p-5 space-y-4">
            
            {/* Dates & Duration */}
            <div className="flex items-center justify-between border-b border-slate-100 pb-4">
              <div className="flex flex-col">
                <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider">Thời gian</span>
                <span className="text-sm font-bold text-slate-800 flex items-center gap-1.5 mt-1">
                  <CalendarDays size={14} className="text-blue-500" />
                  {startDate ? format(startDate, 'dd/MM') : '--'} 
                  <ArrowRight size={12} className="text-slate-300" /> 
                  {endDate ? format(endDate, 'dd/MM') : '--'}
                </span>
              </div>
              <div className="bg-blue-50 text-blue-700 text-xs font-bold px-2.5 py-1 rounded-lg">
                {duration > 0 ? `${duration} Ngày` : '---'}
              </div>
            </div>

            {/* Tags / Badges */}
            <div className="grid grid-cols-2 gap-2">
              <div className="bg-slate-50 border border-slate-100 rounded-xl p-2.5 flex flex-col items-center justify-center gap-1">
                <Users size={14} className="text-slate-400" />
                <span className="text-xs font-bold text-slate-700">{tripSize} Người</span>
              </div>
              <div className="bg-slate-50 border border-slate-100 rounded-xl p-2.5 flex flex-col items-center justify-center gap-1">
                <Wallet size={14} className="text-slate-400" />
                <span className="text-xs font-bold text-slate-700 truncate w-full text-center">{budgetMap[budget]}</span>
              </div>
            </div>

            {/* Selected Interests Note */}
            <AnimatePresence>
              {interests && interests.length > 0 && (
                <motion.div 
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                  className="pt-2"
                >
                   <p className="text-[11px] text-slate-500 font-medium flex items-center gap-1.5">
                      <Heart size={12} className="text-red-400 shrink-0" />
                      <span className="truncate">Sở thích: {interests.map(i => i.name || i.title).join(", ")}</span>
                   </p>
                </motion.div>
              )}
            </AnimatePresence>

          </div>
        </motion.div>
      </div>

      {/* Recent Trips Appendix */}
      <div className="w-full max-w-sm mt-auto pb-4 z-10">
        <h3 className="text-sm font-bold text-slate-500 uppercase tracking-widest mb-3 ml-1 flex items-center justify-between">
          Chuyến đi gần đây
          <button onClick={() => navigate('/my-trips')} className="text-xs text-blue-500 hover:text-blue-700 transition-colors capitalize tracking-normal font-semibold">Xem tất cả</button>
        </h3>
        
        <div className="space-y-2">
          {isRecentTripsLoading ? (
            // Skeleton Loading State
            [...Array(3)].map((_, i) => (
              <div 
                key={`skeleton-${i}`} 
                className="flex items-center gap-3 p-2 rounded-2xl"
              >
                <div className="w-12 h-12 rounded-xl bg-slate-200/60 shrink-0 animate-pulse" />
                <div className="flex-1 space-y-2.5 py-1">
                  <div className="h-3.5 bg-slate-200/60 rounded-md w-3/4 animate-pulse" />
                  <div className="h-2.5 bg-slate-100/60 rounded-md w-1/3 animate-pulse" />
                </div>
              </div>
            ))
          ) : recentTrips.length > 0 ? recentTrips.map(trip => (
            <div 
              key={trip.id}
              onClick={() => navigate(`/trips/${trip.id}`)}
              className="flex items-center gap-3 p-2 rounded-2xl hover:bg-white hover:shadow-sm cursor-pointer transition-all border border-transparent hover:border-slate-200 group"
            >
              <div className="w-12 h-12 rounded-xl overflow-hidden bg-slate-200 shrink-0">
                <img src={trip.image} alt={trip.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
              </div>
              <div className="flex-1 min-w-0">
                <h4 className="text-sm font-bold text-slate-800 line-clamp-1 group-hover:text-blue-600 transition-colors">{trip.title}</h4>
                <p className="text-[11px] text-slate-400 font-medium mt-0.5 capitalize">{trip.time}</p>
              </div>
            </div>
          )) : (
            <div className="text-center py-6 border border-dashed border-slate-200 rounded-2xl bg-slate-50/50">
               <Calendar size={20} className="mx-auto text-slate-300 mb-2" />
               <p className="text-xs font-semibold text-slate-500">Chưa có chuyến đi nào</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

const ArrowRight = ({ className, ...props }) => (
    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className} {...props}>
        <path d="M5 12h14" />
        <path d="m12 5 7 7-7 7" />
    </svg>
);

export default TripPreviewPanel;
