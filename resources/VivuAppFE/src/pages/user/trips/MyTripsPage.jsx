import React, { useState, useMemo } from "react";
import AppNavbar from "../../../components/layout/AppNavbar";
import TripList from "../../../components/user/trips/list/TripList";
import TripListSkeleton from "../../../components/user/trips/list/TripListSkeleton";
import TripFilters from "../../../components/user/trips/list/TripFilters";
import ConfirmationModal from "../../../components/common/modals/ConfirmationModal";
import CreateTripModal from "../../../components/common/modals/CreateTripModal";
import {
  Plus,
  Calendar,
  Map,
  Users,
  ArrowRight,
  MapPin,
  Sparkles,
} from "lucide-react";
import { motion } from "framer-motion";

import toast from "../../../utils/toast";
import ConnectionError from "../../../components/common/ConnectionError";

import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../../../contexts/auth-context";
import useUserTrips from "../../../hooks/trips/useUserTrips";
import useDebounce from "../../../hooks/utils/useDebounce";
import tripApi from "../../../api/tripApi";

const MyTripsPage = () => {
  const navigate = useNavigate();
  const location = useLocation();

  // Handle redirect messages
  const hasToastedRef = React.useRef(false);
  React.useEffect(() => {
    if (location.state?.tripLeftSuccess && !hasToastedRef.current) {
      hasToastedRef.current = true;
      toast.success("Đã rời khỏi chuyến đi thành công");
      // Clear the state so it doesn't show again on reload
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location, navigate]);

  // Use custom hooks
  const { isAuthenticated } = useAuth();
  const {
    ownedTrips: rawOwnedTrips,
    sharedTrips: rawSharedTrips,
    loading,
    error,
    refetch,
    deleteTrip,
    createTrip,
    updateTripVisibility,
    tripLimitInfo,
  } = useUserTrips();

  // Local UI state
  const [filter, setFilter] = useState("all");
  const [activeTab, setActiveTab] = useState("owned"); // "owned" or "shared"
  const [deleteId, setDeleteId] = useState(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  // Debounce search for performance
  const debouncedSearch = useDebounce(searchQuery, 300);

  // Filter trips based on status and search
  const filterTrips = React.useCallback(
    (trips) => {
      return trips.filter((trip) => {
        const matchesStatus = filter === "all" || trip.status === filter;
        const matchesSearch =
          trip.title.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
          trip.location.toLowerCase().includes(debouncedSearch.toLowerCase());
        return matchesStatus && matchesSearch;
      });
    },
    [filter, debouncedSearch],
  );

  const ownedTrips = useMemo(
    () => filterTrips(rawOwnedTrips),
    [rawOwnedTrips, filterTrips],
  );

  const sharedTrips = useMemo(
    () => filterTrips(rawSharedTrips),
    [rawSharedTrips, filterTrips],
  );

  const nextTrip = useMemo(() => {
    if (!rawOwnedTrips || rawOwnedTrips.length === 0) return null;
    return (
      rawOwnedTrips.find(
        (t) => t.status !== "completed" && t.status !== "Completed",
      ) || null
    );
  }, [rawOwnedTrips]);

  const totalTrips = rawOwnedTrips ? rawOwnedTrips.length : 0;
  const totalShared = rawSharedTrips ? rawSharedTrips.length : 0;

  // Handlers
  const handleDeleteClick = (id) => {
    setDeleteId(id);
    setIsDeleteModalOpen(true);
  };

  const confirmDelete = async () => {
    const success = await deleteTrip(deleteId);
    if (success) {
      toast.success("Đã xóa chuyến đi thành công");
    } else {
      toast.error("Không thể xóa chuyến đi");
    }
    setIsDeleteModalOpen(false);
    setDeleteId(null);
  };

  const handleCreateTrip = async (newTrip) => {
    try {
      const createdTrip = await createTrip(newTrip);
      if (createdTrip) {
        toast.success("Đã tạo chuyến đi mới thành công");
        setIsCreateModalOpen(false);
        navigate(`/trips/${createdTrip.id}`);
      }
    } catch (error) {
      console.error("Create trip failed", error);
      const serverMessage = error.response?.data?.message;
      toast.error(serverMessage || "Không thể tạo chuyến đi");
    }
  };

  const handleToggleVisibility = async (tripId, isPublic) => {
    const success = await updateTripVisibility(tripId, isPublic);
    if (success) {
      toast.success(
        isPublic ? "Đã công khai chuyến đi" : "Đã đặt chuyến đi thành riêng tư",
      );
    } else {
      toast.error("Không thể cập nhật trạng thái chuyến đi");
    }
  };

  const handleCreateTripClick = () => {
    if (tripLimitInfo?.hasReachedLimit) {
      toast.error(
        `Bạn đã đạt giới hạn tạo chuyến đi (${tripLimitInfo.numberOfTripCreated}/${tripLimitInfo.tripLimit}). Nâng cấp Premium để tạo không giới hạn!`
      );
      return;
    }
    setIsCreateModalOpen(true);
  };

  // Rating Logic
  const handleRateSubmit = async (tripId, { rating, review }) => {
    try {
      await tripApi.rate(tripId, {
        tripId,
        rating,
        reviewContent: review,
      });
      // Optimistic update or refetch
      refetch(); // Reload list to update isRated status
      toast.success("Cảm ơn bạn đã đánh giá!");
    } catch (error) {
      console.error("Rate trip failed", error);
      toast.error("Lỗi khi gửi đánh giá");
    }
  };



  return (
    <div className="min-h-screen bg-gray-50/30 font-sans text-slate-800 pb-bottom-nav lg:pb-0">
      <AppNavbar />

      {/* Background element - Neutral Texture */}
      <div
        className="fixed inset-0 pointer-events-none z-0 opacity-[0.03]"
        style={{
          backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E")`,
        }}
      ></div>

      <main className="max-w-[1600px] mx-auto px-6 md:px-12 py-12 relative z-10">
        {/* Bento Dashboard Section */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-12">
          {/* Hero Card (Next Trip / Banner) - Span 2 */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="md:col-span-2 relative h-64 md:h-[280px] rounded-[2rem] overflow-hidden group shadow-sm ring-1 ring-slate-200 cursor-pointer"
            onClick={() =>
              nextTrip
                ? navigate(`/trips/${nextTrip.id}`)
                : handleCreateTripClick()
            }
          >
            {nextTrip ? (
              <>
                <div className="absolute inset-0 bg-slate-900">
                  <img
                    src={
                      nextTrip.imageUrl ||
                      nextTrip.image ||
                      "https://images.unsplash.com/photo-1506929562872-bb421503ef21?q=80&w=1200"
                    }
                    alt={nextTrip.title}
                    className="w-full h-full object-cover opacity-60 group-hover:scale-105 group-hover:opacity-75 transition-all duration-700"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-slate-900/90 via-slate-900/40 to-transparent" />
                </div>
                <div className="absolute inset-0 p-6 md:p-8 flex flex-col justify-between text-white">
                  <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/20 backdrop-blur-md w-fit border border-white/10 shadow-sm">
                    <Calendar size={14} className="text-blue-200" />
                    <span className="text-xs font-bold tracking-wider text-blue-50 uppercase shadow-sm">
                      Hành trình tiếp theo
                    </span>
                  </div>

                  <div className="space-y-4">
                    <div>
                      <h2 className="text-3xl md:text-5xl font-black text-white mb-2 tracking-tight group-hover:text-blue-100 transition-colors drop-shadow-md line-clamp-1">
                        {nextTrip.title}
                      </h2>
                      <div className="flex items-center gap-2 text-slate-200 font-medium drop-shadow-sm">
                        <MapPin size={16} />
                        <span className="truncate">
                          {nextTrip.location || "Đang lên kế hoạch"}
                        </span>
                      </div>
                    </div>

                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/trips/${nextTrip.id}`);
                      }}
                      className="flex items-center gap-2 bg-white/90 backdrop-blur-md text-slate-900 px-6 py-3 rounded-full font-bold text-sm w-fit hover:bg-white hover:scale-105 active:scale-95 transition-all shadow-xl"
                    >
                      Xem chi tiết <ArrowRight size={16} />
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <div className="absolute inset-0 bg-gradient-to-br from-blue-950 via-indigo-900 to-slate-900 p-6 md:p-8 flex flex-col justify-between text-white text-center sm:text-left transition-transform group-hover:scale-105 duration-700">
                <div className="absolute inset-0 opacity-20 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-blue-400 via-transparent to-transparent"></div>
                <div className="relative inline-flex items-center md:items-start gap-2 px-3 py-1.5 rounded-full bg-white/10 backdrop-blur-md w-fit mx-auto sm:mx-0 border border-white/10">
                  <Sparkles size={14} className="text-blue-300" />
                  <span className="text-xs font-bold tracking-wider text-blue-100 uppercase">
                    Khám phá thế giới
                  </span>
                </div>
                <div className="relative space-y-4 max-w-lg mb-2 mx-auto sm:mx-0">
                  <h2 className="text-3xl md:text-5xl font-black text-white mb-2 tracking-tight leading-tight">
                    Bắt tay vào hành trình mới ngay?
                  </h2>
                  <p className="text-slate-300 font-medium text-base md:text-lg hidden sm:block">
                    Hàng kho danh lam thắng cảnh và tiện ích đang chờ đón dấu
                    chân của bạn.
                  </p>
                </div>
              </div>
            )}
          </motion.div>

          {/* Right Column: Stats & Action */}
          <div className="flex flex-col gap-4 md:gap-6">
            {/* Stats Split Grid */}
            <div className="grid grid-cols-2 gap-4 md:gap-6 flex-1">
              <motion.div
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: 0.1 }}
                className="bg-white/80 backdrop-blur-xl rounded-[2rem] p-5 flex flex-col justify-center items-center sm:items-start text-center sm:text-left ring-1 ring-slate-200/80 shadow-[0_8px_30px_rgb(0,0,0,0.04)] hover:shadow-[0_8px_30px_rgb(0,0,0,0.08)] transition-all duration-300 group"
              >
                <div className="w-10 h-10 bg-blue-50 text-blue-600 rounded-2xl flex items-center justify-center mb-3 group-hover:rotate-12 transition-transform shadow-sm ring-1 ring-blue-100">
                  <Map size={20} />
                </div>
                <span className="text-3xl lg:text-4xl font-black text-slate-800 tracking-tight">
                  {totalTrips}
                </span>
                <span className="text-[10px] sm:text-xs font-bold text-slate-500 mt-1 uppercase tracking-wider">
                  Chuyến đi
                </span>
              </motion.div>

              <motion.div
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: 0.2 }}
                className="bg-white/80 backdrop-blur-xl rounded-[2rem] p-5 flex flex-col justify-center items-center sm:items-start text-center sm:text-left ring-1 ring-slate-200/80 shadow-[0_8px_30px_rgb(0,0,0,0.04)] hover:shadow-[0_8px_30px_rgb(0,0,0,0.08)] transition-all duration-300 group"
              >
                <div className="w-10 h-10 bg-indigo-50 text-indigo-600 rounded-2xl flex items-center justify-center mb-3 group-hover:rotate-12 transition-transform shadow-sm ring-1 ring-indigo-100">
                  <Users size={20} />
                </div>
                <span className="text-3xl lg:text-4xl font-black text-slate-800 tracking-tight">
                  {totalShared}
                </span>
                <span className="text-[10px] sm:text-xs font-bold text-slate-500 mt-1 uppercase tracking-wider">
                  Chia sẻ
                </span>
              </motion.div>
            </div>

            {/* Action Box: Thêm mới */}
            <motion.button
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.3 }}
              onClick={handleCreateTripClick}
              className="group relative h-[88px] md:h-[110px] bg-gradient-primary rounded-[2rem] drop-shadow-xl p-5 md:p-6 flex items-center justify-between overflow-hidden transition-transform active:scale-95 w-full shrink-0"
            >
              <div className="absolute inset-0 bg-white/20 translate-y-full group-hover:translate-y-0 transition-transform duration-500 ease-out" />
              <div className="relative z-10 text-left">
                <h3 className="text-white font-black text-xl md:text-2xl drop-shadow-md tracking-tight">
                  Tạo mới
                </h3>
                <p className="text-blue-100/90 text-sm font-semibold mt-0.5 hidden sm:block">
                  Lên lịch ngay
                </p>
              </div>
              <div className="relative z-10 w-12 h-12 bg-white/95 text-blue-600 rounded-2xl flex items-center justify-center shadow-lg group-hover:bg-white group-hover:rotate-90 transition-all duration-500 border border-blue-100 shrink-0">
                <Plus size={24} className="stroke-3" />
              </div>
            </motion.button>
          </div>
        </div>

        {/* Filters */}
        <TripFilters
          currentFilter={filter}
          onFilterChange={setFilter}
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
        />

        {/* List */}
        {error ? (
          <ConnectionError
            message={error}
            onRetry={refetch}
            isRetrying={loading}
          />
        ) : loading ? (
          <TripListSkeleton count={4} />
        ) : (
          <div className="space-y-6">
            {/* iOS style Segmented Control Tabs */}
            {rawSharedTrips?.length > 0 && (
              <div className="flex bg-slate-200/60 hover:bg-slate-200/80 transition-colors p-1.5 rounded-2xl w-full sm:w-fit mx-auto sm:mx-0">
                <button
                  onClick={() => setActiveTab("owned")}
                  className={`flex-1 sm:flex-none flex justify-center items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold transition-all duration-300 ${
                    activeTab === "owned"
                      ? "bg-white text-slate-800 shadow-sm ring-1 ring-slate-200/50"
                      : "text-slate-500 hover:text-slate-700 hover:bg-slate-300/30"
                  }`}
                >
                  Của tôi{" "}
                  <span
                    className={`px-2 py-0.5 rounded-lg text-xs ${activeTab === "owned" ? "bg-slate-100/80 text-slate-600" : "bg-slate-300/50 text-slate-500"}`}
                  >
                    {rawOwnedTrips?.length || 0}
                  </span>
                </button>
                <button
                  onClick={() => setActiveTab("shared")}
                  className={`flex-1 sm:flex-none flex justify-center items-center gap-2 px-6 py-2.5 rounded-xl text-sm font-bold transition-all duration-300 ${
                    activeTab === "shared"
                      ? "bg-white text-slate-800 shadow-sm ring-1 ring-slate-200/50"
                      : "text-slate-500 hover:text-slate-700 hover:bg-slate-300/30"
                  }`}
                >
                  Được chia sẻ{" "}
                  <span
                    className={`px-2 py-0.5 rounded-lg text-xs ${activeTab === "shared" ? "bg-slate-100/80 text-slate-600" : "bg-slate-300/50 text-slate-500"}`}
                  >
                    {rawSharedTrips?.length || 0}
                  </span>
                </button>
              </div>
            )}

            {/* Active List Content */}
            <motion.div
              key={activeTab}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.3 }}
            >
              {activeTab === "owned" ? (
                <div>
                  <TripList
                    key={`owned-${filter}-${ownedTrips.length}`}
                    trips={ownedTrips}
                    onDelete={handleDeleteClick}
                    onRateSubmit={handleRateSubmit}
                    onToggleVisibility={handleToggleVisibility}
                  />
                </div>
              ) : (
                <div>
                  <TripList
                    key={`shared-${filter}-${sharedTrips.length}`}
                    trips={sharedTrips}
                    onDelete={handleDeleteClick}
                    onRateSubmit={handleRateSubmit}
                    onToggleVisibility={handleToggleVisibility}
                  />
                </div>
              )}
            </motion.div>
          </div>
        )}
      </main>

      <ConfirmationModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={confirmDelete}
        title="Xóa chuyến đi"
        message="Bạn có chắc chắn muốn xóa chuyến đi này không? Hành động này không thể hoàn tác."
        confirmText="Xóa bỏ"
        cancelText="Hủy"
      />

      <CreateTripModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreate={handleCreateTrip}
      />
    </div>
  );
};

export default MyTripsPage;
