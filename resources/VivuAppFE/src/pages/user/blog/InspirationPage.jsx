import React, { useState, lazy, Suspense, useMemo } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { AnimatePresence } from "framer-motion";
import AppNavbar from "../../../components/layout/AppNavbar";
import { Search, PenSquare, Sparkles, Map, FileText } from "lucide-react";
import { motion as Motion } from "framer-motion";

import {
  usePublicTrips,
  useSearchPublicTrips,
} from "../../../hooks/trips/usePublicTrips";
import { transformAndFilterTrips } from "../../../utils/tripTransformers";
import {
  usePublicBlogs,
  useSearchPublicBlogs,
  useMyBookmarks,
  useMyDrafts,
} from "../../../hooks/blogs/useBlogs";
import useSWR from "swr";
import userApi from "../../../api/userApi";
import { useAuth } from "../../../contexts/auth-context";

// Lazy Imports
const BlogFeedSection = lazy(() => import("./sections/BlogFeedSection"));
const TripFeedSection = lazy(() => import("./sections/TripFeedSection"));
const DraftPickerDialog = lazy(
  () => import("../../../components/user/blog/DraftPickerDialog"),
);

const InspirationPage = () => {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const initialTab = searchParams.get("tab") || "blogs";

  const [activeTab, setActiveTab] = useState(initialTab); // "blogs" | "trips"
  const [searchQuery, setSearchQuery] = useState("");
  const [tripPage, setTripPage] = useState(1);
  const [blogPage, setBlogPage] = useState(1);
  const { user } = useAuth();

  // Sync activeTab with searchParams when they change
  React.useEffect(() => {
    const tabParam = searchParams.get("tab");
    if (tabParam && (tabParam === "blogs" || tabParam === "trips")) {
      setActiveTab(tabParam);
    }
  }, [searchParams]);

  // Fetch user favorites
  const { data: favoriteTripsResp } = useSWR(
    user ? "my-favorite-trips" : null,
    async () => {
      const res = await userApi.getMyFavoriteTrips({ pageSize: 100 });
      return res.data;
    },
    { revalidateOnFocus: false },
  );

  const favoriteTripIds = new Set(
    (favoriteTripsResp?.items || favoriteTripsResp || []).map((t) =>
      t.id?.toLowerCase(),
    ),
  );

  // Fetch user blog bookmarks
  const { data: bookmarkedBlogsResp } = useMyBookmarks(1, 100);
  const bookmarkedBlogIds = useMemo(() => {
    return new Set(
      (bookmarkedBlogsResp?.items || []).map((b) => b.id?.toLowerCase()),
    );
  }, [bookmarkedBlogsResp]);

  // Trips Data
  const { data: tripFeedData, isLoading: tripFeedLoading } = usePublicTrips(
    activeTab === "trips" && searchQuery?.length < 2
      ? { sortBy: "newest", pageSize: 15, pageNumber: tripPage }
      : null,
  );

  // Search API trips (with pagination parameters)
  const { data: searchData, isLoading: searchLoading } = useSearchPublicTrips(
    activeTab === "trips" ? searchQuery : null,
    15, // pageSize = 15 to fit 5 items/row
    tripPage,
  );

  // Search API blogs
  const { data: blogSearchData, isLoading: blogSearchLoading } = useSearchPublicBlogs(
    activeTab === "blogs" ? searchQuery : null,
    15,
    blogPage,
  );

  const isSearching = searchQuery?.length >= 2;
  const tripsToDisplay =
    isSearching && searchData ? searchData.items : tripFeedData?.items || [];

  const displayTrips = transformAndFilterTrips(tripsToDisplay);

  const handleTabChange = (tab) => {
    setActiveTab(tab);
    setSearchQuery("");
    setBlogPage(1);
    
    // Support URL routing
    const newParams = new URLSearchParams(searchParams);
    newParams.set("tab", tab);
    setSearchParams(newParams, { replace: true });
  };

  const [showDraftPicker, setShowDraftPicker] = useState(false);
  const { data: myDrafts } = useMyDrafts();
  const hasDrafts = (myDrafts?.length || 0) > 0;

  const handleWriteClick = React.useCallback(() => {
    if (hasDrafts && user) {
      setShowDraftPicker(true);
    } else {
      navigate("/inspiration/create");
    }
  }, [hasDrafts, navigate, user]);

  return (
    <div className="flex flex-col min-h-screen bg-white pb-28 md:pb-8 lg:pb-0">
      <AppNavbar />

      <main className="flex-1 w-full max-w-[1600px] mx-auto px-4 sm:px-6 md:px-12 py-6 sm:py-8 lg:py-12 flex flex-col gap-8">
        {/* HEADER SECTION (Title & Actions) */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <h1 className="text-3xl sm:text-4xl lg:text-[40px] font-black text-gray-900 tracking-tight">
            Trải nghiệm
          </h1>
          {/* Action Area */}
          <div className="flex items-center gap-2 sm:gap-3 w-full sm:w-auto">
            <Motion.button
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={() => navigate("/inspiration/me")}
              className="flex items-center justify-center gap-1.5 sm:gap-2 px-3 py-2 sm:px-4 sm:py-2.5 border border-gray-200 text-gray-700 font-bold rounded-full hover:bg-gray-50 transition-all whitespace-nowrap cursor-pointer text-xs sm:text-sm"
            >
              <FileText size={16} />
              <span>Bài viết của tôi</span>
            </Motion.button>
            <Motion.button
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={handleWriteClick}
              className="flex items-center justify-center gap-1.5 sm:gap-2 px-4 py-2 sm:px-5 sm:py-2.5 bg-gradient-primary text-white font-bold rounded-full hover:bg-blue-700 transition-all whitespace-nowrap cursor-pointer text-xs sm:text-sm"
            >
              <PenSquare size={16} />
              <span>Viết bài</span>
            </Motion.button>
          </div>
        </div>

        {/* SEARCH & TABS ROW */}
        <div className="flex flex-col md:flex-row items-center gap-3 sm:gap-4 w-full">
          {/* Search Bar */}
          <div className="relative flex-1 w-full flex items-center bg-gray-100 rounded-full py-3 px-5 focus-within:ring-2 focus-within:ring-blue-500 focus-within:bg-white transition-all shadow-sm">
            <Search className="w-5 h-5 text-gray-400 mr-3 shrink-0" />
            <input
              type="text"
              placeholder="Tìm kiếm địa điểm hoặc tên bài viết..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="bg-transparent border-none outline-none w-full text-[15px] text-gray-800 placeholder-gray-400"
            />
            {isSearching && ((activeTab === "trips" && searchLoading) || (activeTab === "blogs" && blogSearchLoading)) && (
              <div className="w-4 h-4 ml-2 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin shrink-0" />
            )}
          </div>

          {/* Tabs Pill */}
          <div className="flex bg-gray-100 p-1 rounded-full w-full md:w-auto shrink-0 self-start md:self-auto shadow-inner relative">
            <button
              onClick={() => handleTabChange("blogs")}
              className={`relative z-10 flex-1 md:flex-none px-6 py-2 rounded-full text-sm font-bold transition-colors flex items-center justify-center gap-2 ${
                activeTab === "blogs"
                  ? "text-gray-900"
                  : "text-gray-500 hover:text-gray-700 hover:bg-gray-50"
              }`}
            >
              {activeTab === "blogs" && (
                <Motion.div
                  layoutId="inspirationTabPill"
                  className="absolute inset-0 bg-white rounded-full shadow-sm border border-gray-200 z-[-1]"
                  transition={{ type: "spring", bounce: 0.2, duration: 0.6 }}
                />
              )}
              {activeTab !== "blogs" && (
                <FileText size={16} className="inline-block" />
              )}
              Bài viết
            </button>
            <button
              onClick={() => handleTabChange("trips")}
              className={`relative z-10 flex-1 md:flex-none px-6 py-2 rounded-full text-sm font-bold transition-colors flex items-center justify-center gap-2 ${
                activeTab === "trips"
                  ? "text-gray-900"
                  : "text-gray-500 hover:text-gray-700 hover:bg-gray-50"
              }`}
            >
              {activeTab === "trips" && (
                <Motion.div
                  layoutId="inspirationTabPill"
                  className="absolute inset-0 bg-white rounded-full shadow-sm border border-gray-200 z-[-1]"
                  transition={{ type: "spring", bounce: 0.2, duration: 0.6 }}
                />
              )}
              {activeTab !== "trips" && (
                <Map size={16} className="inline-block" />
              )}
              Lịch trình
            </button>
          </div>
        </div>

        {/* FEED CONTENT */}
        <AnimatePresence mode="wait">
          <Motion.div
            key={activeTab}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.2 }}
            className="w-full"
          >
            {/* MAIN FEED */}
            {activeTab === "blogs" ? (
              <Suspense
                fallback={
                  <div className="h-96 animate-pulse bg-gray-50 rounded-2xl" />
                }
              >
                <BlogFeedSection
                  searchQuery={searchQuery}
                  onSearchChange={(q) => {
                    setSearchQuery(q);
                    setBlogPage(1);
                  }}
                  bookmarkedBlogIds={bookmarkedBlogIds}
                  searchData={blogSearchData}
                  searchLoading={blogSearchLoading}
                  currentPage={blogPage}
                  onPageChange={setBlogPage}
                />
              </Suspense>
            ) : (
              <Suspense
                fallback={
                  <div className="h-96 animate-pulse bg-gray-50 rounded-2xl" />
                }
              >
                <TripFeedSection
                  trips={displayTrips}
                  loading={
                    searchQuery?.length >= 2 ? searchLoading : tripFeedLoading
                  }
                  favoriteTripIds={favoriteTripIds}
                  currentPage={tripPage}
                  totalPages={
                    searchQuery?.length >= 2
                      ? (searchData?.totalPages ?? 1)
                      : (tripFeedData?.totalPages ?? 1)
                  }
                  onPageChange={setTripPage}
                  searchQuery={searchQuery}
                  onSearchChange={(q) => {
                    setSearchQuery(q);
                    setTripPage(1); // Reset page on search
                  }}
                />
              </Suspense>
            )}
          </Motion.div>
        </AnimatePresence>
      </main>

      {/* Modals */}
      <Suspense fallback={null}>
        <DraftPickerDialog
          isOpen={showDraftPicker}
          drafts={myDrafts}
          onClose={() => setShowDraftPicker(false)}
        />
      </Suspense>
    </div>
  );
};

export default InspirationPage;
