import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import {
  MapPin,
  Camera,
  Star,
  Settings,
  Heart,
  Plane,
  ImageIcon,
  MapPinPlus,
  Bookmark,
  User,
} from "lucide-react";
import { motion as Motion, AnimatePresence } from "framer-motion";
import AppNavbar from "../../../components/layout/AppNavbar";

import SavedTripsTab from "../../../components/user/profile/SavedTripsTab";
import SavedBlogsTab from "../../../components/user/profile/SavedBlogsTab";
import EditProfileModal from "../../../components/user/profile/EditProfileModal";
import userApi from "../../../api/userApi";
import { useUpdateProfile, useUploadAvatar } from "../../../hooks/users/useUsers";
import { useUser } from "../../../hooks/useUsers";
import { useUserTrips } from "../../../hooks/trips/useTrips";
import { useAuth } from "../../../contexts/auth-context";
import { EmptyState } from "../../../components/common";
import { TripGridSkeleton } from "../../../components/ui/TripCardSkeleton";
import { format, parseISO } from "date-fns";

const ProfilePage = () => {
  const navigate = useNavigate();
  const { userId: authUserId } = useAuth();
  const { updateProfile } = useUpdateProfile();
  const { uploadAvatar } = useUploadAvatar();
  const { trips: tripsArray, isLoading: isTripsLoading } =
    useUserTrips(authUserId);
  const [activeTab, setActiveTab] = useState("trips");
  const [showEditModal, setShowEditModal] = useState(false);
  const [user, setUser] = useState({
    name: "",
    handle: "",
    avatar: null,
    cover: "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?ixlib=rb-4.0.3&auto=format&fit=crop&w=2000&q=80",
    bio: "",
    stats: { followers: 0, following: 0, trips: 0, places: 0 }
  });
  const tabs = [
    { id: "trips", label: "Chuyến đi", icon: Plane },
    { id: "saved", label: "Lịch trình đã lưu", icon: Heart },
    { id: "saved-blogs", label: "Bài viết đã lưu", icon: Bookmark },
  ];

  const { data: userData, isLoading: isUserLoading } = useUser(authUserId);

  useEffect(() => {
    if (userData) {
      setUser((prev) => ({
        ...prev,
        name: userData.fullName || userData.email || prev.name,
        handle: userData.email || prev.handle,
        avatar: userData.avatarUrl || prev.avatar,
        bio: userData.bio || prev.bio,
        stats: {
          followers: userData.followersCount || prev.stats.followers,
          following: userData.followingCount || prev.stats.following,
          trips: tripsArray?.length || prev.stats.trips,
          places: userData.placesCount || prev.stats.places,
        }
      }));
    }
  }, [userData, tripsArray]);

  const handleSaveProfile = async (formData) => {
    try {
      let avatarUrl = undefined;

      // If avatarUrl is a base64 data URI or a File, upload it first
      if (formData.avatarUrl) {
        if (formData.avatarUrl instanceof File) {
          const uploadRes = await uploadAvatar(formData.avatarUrl);
          avatarUrl = uploadRes?.data || uploadRes;
        } else if (typeof formData.avatarUrl === "string" && formData.avatarUrl.startsWith("data:")) {
          const res = await fetch(formData.avatarUrl);
          const blob = await res.blob();
          const file = new File([blob], "avatar.png", { type: blob.type });
          const uploadRes = await uploadAvatar(file);
          avatarUrl = uploadRes?.data || uploadRes;
        } else {
          avatarUrl = formData.avatarUrl;
        }
      }

      const payload = {
        fullName: formData.fullName || undefined,
        bio: formData.bio || undefined,
        avatarUrl: avatarUrl,
        phone: formData.phone || undefined,
        gender: formData.gender || undefined,
        dateOfBirth: formData.dateOfBirth || undefined,
      };

      const response = await updateProfile(payload);

      if (response?.success && response?.data) {
        const updatedData = response.data;
        setUser((prev) => ({
          ...prev,
          name: updatedData.fullName || prev.name,
          bio: updatedData.bio || prev.bio,
          avatar: updatedData.avatarUrl || prev.avatar,
        }));
      } else {
        // Fallback: update local state even if API response is unexpected
        setUser((prev) => ({
          ...prev,
          bio: formData.bio || prev.bio,
        }));
      }
    } catch (error) {
      console.error("Failed to update profile:", error);
      // Still update local state for optimistic UI
      setUser((prev) => ({
        ...prev,
        bio: formData.bio || prev.bio,
      }));
    }
  };


  return (
    <div className="flex flex-col min-h-screen bg-gray-50 pb-bottom-nav lg:pb-0">
      <AppNavbar />

      <div className="flex-1 pb-20">
        {/* Hero / Cover Section */}
        <div className="relative h-60 md:h-80 w-full group">
          <Motion.img
            initial={{ scale: 1.1 }}
            animate={{ scale: 1 }}
            transition={{ duration: 0.8 }}
            src={user.cover}
            alt="Cover"
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-linear-to-b from-black/10 via-transparent to-black/60" />

          {/* Edit Cover Button */}
          <button
            onClick={() => setShowEditModal(true)}
            className="absolute bottom-4 right-4 p-2 bg-black/40 backdrop-blur-md rounded-lg text-white opacity-0 group-hover:opacity-100 transition-opacity hover:bg-black/60 cursor-pointer"
          >
            <Camera size={18} />
          </button>
        </div>

        {/* Profile Info Container */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 -mt-20 relative z-10">
          <div className="bg-white rounded-[2rem] shadow-sm border border-gray-100 p-6 md:p-8 relative overflow-hidden">
            <div className="relative z-10 flex flex-col md:flex-row items-start md:items-end gap-6">
              {isUserLoading ? (
                // --- Skeleton Header ---
                <>
                  <div className="relative shrink-0 animate-pulse">
                    <div className="w-28 h-28 md:w-36 md:h-36 rounded-full border-4 border-white shadow-xl bg-slate-200 ring-4 ring-blue-50" />
                  </div>
                  <div className="flex-1 w-full animate-pulse">
                    <div className="flex items-start justify-between mb-2">
                      <div className="space-y-3">
                        <div className="h-8 w-48 md:h-10 md:w-64 bg-slate-200 rounded-lg" />
                        <div className="h-4 w-32 bg-slate-100 rounded-md" />
                      </div>
                      <div className="h-10 w-10 bg-slate-100 rounded-full" />
                    </div>
                    <div className="space-y-2 mt-4 max-w-lg">
                      <div className="h-4 w-full bg-slate-100 rounded-md" />
                      <div className="h-4 w-3/4 bg-slate-100 rounded-md" />
                    </div>
                  </div>
                </>
              ) : (
                // --- Actual Content ---
                <>
                  <Motion.div
                    initial={{ scale: 0.8, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    className="relative shrink-0"
                  >
                    <div className="w-28 h-28 md:w-36 md:h-36 rounded-full border-4 border-white shadow-xl overflow-hidden relative ring-4 ring-blue-100">
                      {user.avatar ? (
                        <img
                          src={user.avatar}
                          alt={user.name}
                          className="w-full h-full object-cover"
                          referrerPolicy="no-referrer"
                        />
                      ) : (
                        <div className="w-full h-full bg-linear-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white text-3xl font-bold">
                          {user.name?.charAt(0) || <User size={40} />}
                        </div>
                      )}
                    </div>
                  </Motion.div>

                  <div className="flex-1 w-full">
                    <div className="flex items-start justify-between mb-2">
                      <div>
                        <h1 className="text-2xl md:text-3xl font-black text-gray-900 tracking-tight">
                          {user.name}
                        </h1>
                        <p className="text-gray-500 font-medium text-sm">
                          {user.handle}
                        </p>
                      </div>

                      {/* Action Buttons */}
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => navigate("/settings")}
                          className="p-2.5 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-full transition-colors"
                          title="Cài đặt"
                        >
                          <Settings size={20} />
                        </button>
                      </div>
                    </div>

                    <p className="text-gray-600 mb-4 max-w-lg leading-relaxed">
                      {user.bio}
                    </p>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Content Tabs */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-10">
          {/* Tab Headers */}
          <div className="flex items-center border-b border-gray-200 overflow-x-auto no-scrollbar mb-8">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-6 py-4 text-sm font-bold tracking-wide transition-colors relative whitespace-nowrap ${
                  activeTab === tab.id
                    ? "text-blue-600"
                    : "text-gray-500 hover:text-gray-700"
                }`}
              >
                <tab.icon size={18} />
                {tab.label}
                {activeTab === tab.id && (
                  <Motion.div
                    layoutId="tab-indicator"
                    className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600"
                  />
                )}
              </button>
            ))}
          </div>

          {/* Tab Content */}
          <AnimatePresence mode="wait">
            <Motion.div
              key={activeTab}
              initial={{ opacity: 0, y: 15 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -15 }}
              transition={{ duration: 0.3 }}
              className="min-h-[400px]"
            >
              {activeTab === "trips" && (
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                  {isTripsLoading ? (
                    <TripGridSkeleton count={4} />
                  ) : tripsArray.length > 0 ? (
                    tripsArray.map((trip) => {
                      const formattedDate = trip.startDate
                        ? format(parseISO(trip.startDate), "MMM yyyy")
                        : "Đang lên kế hoạch";
                      
                      return (
                        <Motion.div
                          key={trip.id}
                          whileHover={{ y: -4 }}
                          onClick={() => navigate(`/trips/${trip.id}`)}
                          className="group relative rounded-2xl overflow-hidden shadow-sm border border-gray-100 hover:shadow-xl transition-all cursor-pointer bg-white"
                        >
                          <div className="h-48 w-full bg-gray-200 relative">
                            <img
                              src={trip.coverUrl || "https://images.unsplash.com/photo-1544885935-98dd03b09034?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80"}
                              alt={trip.title}
                              className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                            />
                            <div className="absolute inset-0 bg-black/20 group-hover:bg-black/10 transition-colors" />
                          </div>
                          <div className="absolute bottom-0 left-0 right-0 p-4 bg-linear-to-t from-black/80 to-transparent pt-12">
                            <h3 className="text-white font-bold text-lg mb-1 truncate">
                              {trip.title}
                            </h3>
                            <div className="flex items-center justify-between text-white/80 text-xs font-medium">
                              <span>{formattedDate}</span>
                              <span className="flex items-center gap-1">
                                <MapPin size={12} /> {trip.cityName || "Địa điểm"}
                              </span>
                            </div>
                          </div>
                        </Motion.div>
                      );
                    })
                  ) : (
                    <div className="col-span-full">
                      <EmptyState
                        icon={Plane}
                        title="Bạn chưa có chuyến đi nào"
                        subtitle="Hãy bắt đầu tạo chuyến đi đầu tiên để khám phá thế giới!"
                      />
                    </div>
                  )}
                  {/* Create New Trip Button */}
                  {!isTripsLoading && (
                    <button
                      onClick={() => navigate("/chat")}
                      className="border-2 border-dashed border-gray-200 rounded-2xl flex flex-col items-center justify-center p-8 gap-3 text-gray-400 hover:border-blue-500 hover:text-blue-500 hover:bg-blue-50/50 transition-all cursor-pointer group min-h-[200px]"
                    >
                      <div className="w-12 h-12 rounded-full flex items-center justify-center bg-gray-50 group-hover:bg-blue-100 transition-colors">
                        <Plane size={20} />
                      </div>
                      <span className="font-bold">Tạo chuyến đi mới</span>
                    </button>
                  )}
                </div>
              )}


              {activeTab === "saved" && <SavedTripsTab />}

              {activeTab === "saved-blogs" && <SavedBlogsTab />}
            </Motion.div>
          </AnimatePresence>
        </div>
      </div>

      {/* Edit Profile Modal */}
      <EditProfileModal
        isOpen={showEditModal}
        onClose={() => setShowEditModal(false)}
        user={user}
        onSave={handleSaveProfile}
      />
    </div>
  );
};

export default ProfilePage;
