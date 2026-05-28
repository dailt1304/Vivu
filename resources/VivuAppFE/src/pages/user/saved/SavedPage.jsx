import React, { useState, Suspense, lazy } from "react";
import { useNavigate } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";

import {
  Search,
  Plus,
  Bookmark,
  Map,
  Grid,
  FolderHeart,
  Loader2,
} from "lucide-react";

import AppNavbar from "../../../components/layout/AppNavbar";
import CollectionCard from "../../../components/user/saved/CollectionCard";
import SavedBlogCard from "../../../components/user/saved/SavedBlogCard";
import SavedTripCard from "../../../components/user/saved/SavedTripCard";
import EmptyState from "../../../components/user/saved/EmptyState";
import toast from "../../../utils/toast";
import useDebounce from "../../../hooks/utils/useDebounce";

import {
  useUserCollections,
  useCreateCollection,
  useUpdateCollection,
  useDeleteCollection,
} from "../../../hooks/collections";
import { useSavedBlogs } from "../../../hooks/blogs/useSavedBlogs";
import { useSavedTrips } from "../../../hooks/trips/useSavedTrips";
import blogApi from "../../../api/blogApi";
import tripApi from "../../../api/tripApi";

// Lazy loading modals
const CreateCollectionModal = lazy(
  () => import("../../../components/user/saved/CreateCollectionModal"),
);
const ConfirmationModal = lazy(
  () => import("../../../components/common/modals/ConfirmationModal"),
);

const TABS = [
  { id: "collections", icon: FolderHeart, label: "Bộ sưu tập" },
  { id: "blogs", icon: Bookmark, label: "Bài viết" },
  { id: "trips", icon: Map, label: "Chuyến đi" },
];

const containerVariants = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.1 } },
  exit: { opacity: 0 }
};

const itemVariants = {
  hidden: { opacity: 0, y: 30, scale: 0.95 },
  show: { 
    opacity: 1, 
    y: 0, 
    scale: 1,
    transition: { type: "spring", stiffness: 350, damping: 25 }
  },
  exit: { opacity: 0, y: 15, scale: 0.95 }
};

const LoadingSkeleton = () => (
  <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-x-8 md:gap-y-12">
    {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
      <div key={i} className="flex flex-col gap-4 animate-pulse">
        <div className="w-full aspect-[4/3] bg-slate-200/60 rounded-3xl"></div>
        <div className="space-y-2.5 px-1">
          <div className="h-4 bg-slate-200/60 rounded-md w-3/4"></div>
          <div className="h-3 bg-slate-200/60 rounded-md w-1/2"></div>
        </div>
      </div>
    ))}
  </div>
);

const SavedPage = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState("collections");
  const [searchQuery, setSearchQuery] = useState("");
  const debouncedSearch = useDebounce(searchQuery, 300);
  const [activeMenuId, setActiveMenuId] = useState(null);

  // Pagination states
  const [collectionsPage] = useState(1);
  const [blogsPage] = useState(1);
  const [tripsPage] = useState(1);

  // SWR Hooks
  const {
    data: collectionsData,
    isLoading: isLoadingCollections,
    mutate: mutateCollections,
  } = useUserCollections(collectionsPage, 16, debouncedSearch);
  const {
    data: blogsData,
    isLoading: isLoadingBlogs,
    mutate: mutateBlogs,
  } = useSavedBlogs(blogsPage, 12);
  const {
    data: tripsData,
    isLoading: isLoadingTrips,
    mutate: mutateTrips,
  } = useSavedTrips(tripsPage, 12);

  const collections = collectionsData?.items || [];
  const blogs = blogsData?.items || [];
  const trips = tripsData?.items || [];

  // Mutations
  const { trigger: createCollection, isMutating: isCreating } =
    useCreateCollection();
  const { trigger: updateCollection, isMutating: isUpdating } =
    useUpdateCollection();
  const { trigger: deleteCollection, isMutating: isDeleting } =
    useDeleteCollection();

  // Modal States
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingCollection, setEditingCollection] = useState(null);
  const [deleteId, setDeleteId] = useState(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  // Close menu on click outside
  React.useEffect(() => {
    const handleClickOutside = () => setActiveMenuId(null);
    window.addEventListener("click", handleClickOutside, { passive: true });
    return () => window.removeEventListener("click", handleClickOutside);
  }, []);

  const handleCreateOrUpdate = async (formData, id) => {
    try {
      if (id) {
        await updateCollection({ id, formData });
        toast.success("Đã cập nhật bộ sưu tập");
      } else {
        await createCollection(formData);
        toast.success("Đã tạo bộ sưu tập mới");
      }
      setIsModalOpen(false);
      setEditingCollection(null);
      // Re-fetch
      mutateCollections();
    } catch (error) {
      toast.error(error?.response?.data?.message || "Có lỗi xảy ra");
    }
  };

  const openCreateModal = () => {
    setEditingCollection(null);
    setIsModalOpen(true);
  };

  const openEditModal = (collection) => {
    setEditingCollection(collection);
    setIsModalOpen(true);
    setActiveMenuId(null);
  };

  const confirmDelete = async () => {
    try {
      await deleteCollection(deleteId);
      toast.success("Đã xóa bộ sưu tập");
      setIsDeleteModalOpen(false);
      mutateCollections();
    } catch (error) {
      toast.error(error?.response?.data?.message || "Có lỗi xảy ra");
    }
  };

  const handleUnbookmarkBlog = async (blogId) => {
    try {
      await blogApi.bookmark(blogId);
      toast.success("Đã bỏ lưu bài viết");
      mutateBlogs();
    } catch {
      toast.error("Không thể bỏ lưu bài viết");
    }
  };

  const handleUnfavoriteTrip = async (tripId) => {
    try {
      await tripApi.favorite(tripId);
      toast.success("Đã bỏ thích chuyến đi");
      mutateTrips();
    } catch {
      toast.error("Không thể bỏ thích chuyến đi");
    }
  };

  const handleCopyTrip = async (tripId) => {
    try {
      await tripApi.copy(tripId);
      toast.success("Đã tạo chuyến đi mới từ bản sao");
      navigate("/trips");
    } catch {
      toast.error("Không thể sao chép chuyến đi");
    }
  };

  // Server-side filtering applied via debouncedSearch for collections
  const filteredCollections = collections;

  // Client-side filtering via search bar for blogs and trips
  const filteredBlogs = blogs.filter(
    (b) =>
      b.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      b.authorName?.toLowerCase().includes(searchQuery.toLowerCase()),
  );
  const filteredTrips = trips.filter(
    (t) =>
      t.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      t.cityName?.toLowerCase().includes(searchQuery.toLowerCase()),
  );

  return (
    <div className="min-h-screen bg-[#fafafa] font-sans text-slate-800 pb-bottom-nav lg:pb-0">
      <AppNavbar />

      <main className="max-w-[1600px] mx-auto px-6 md:px-12 py-12 w-full overflow-hidden">
        {/* Header Area */}
        <div className="mb-10">
          <h1 className="text-4xl md:text-5xl font-black text-slate-900 tracking-tight leading-tight mb-3">
            Đã lưu
          </h1>
          <p className="text-slate-500 text-base md:text-lg font-medium max-w-lg leading-relaxed">
            Kho lưu trữ không gian, ý tưởng và những chuyến đi chờ được hiện thực hóa.
          </p>
        </div>

        {/* Filters/Tabs exactly matching screenshot design */}
        <div className="flex flex-col md:flex-row items-center justify-between gap-6 mb-12">
          {/* Search */}
          <div className="relative w-full md:w-[400px] group">
            <Search className="absolute left-5 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 group-focus-within:text-blue-500 transition-colors" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm kiếm..."
              className="w-full pl-12 pr-6 py-3.5 bg-white border border-slate-100 rounded-full shadow-[0_2px_8px_rgba(0,0,0,0.02)] text-sm font-medium focus:outline-none focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 transition-all hover:bg-slate-50/50 hover:shadow-sm"
            />
          </div>

          {/* Segmented Control Tabs */}
          <div className="w-full md:w-auto pb-2 md:pb-0">
            <div className="bg-slate-50/80 p-[5px] flex md:inline-flex rounded-full gap-1 border border-slate-100 shadow-[inset_0_2px_4px_rgba(0,0,0,0.02)]">
              {TABS.map((tab) => {
                const isActive = activeTab === tab.id;
                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={`relative flex-1 md:flex-none flex items-center justify-center gap-2 px-2 sm:px-4 md:px-7 py-2.5 rounded-full text-[13px] md:text-sm transition-all duration-300 z-10 ${
                      isActive
                        ? "text-blue-600 font-bold"
                        : "text-slate-500 hover:text-slate-700 hover:bg-slate-200/50 font-semibold"
                    }`}
                  >
                    {isActive && (
                      <motion.div
                        layoutId="activeFilter"
                        className="absolute inset-0 bg-white rounded-full shadow-[0_2px_8px_-2px_rgba(0,0,0,0.08)] ring-1 ring-slate-900/5"
                        transition={{ type: "spring", bounce: 0.25, duration: 0.6 }}
                      />
                    )}
                    <span className="relative z-10 flex items-center gap-1.5 md:gap-2 tracking-wide whitespace-nowrap">
                      {tab.icon && <tab.icon size={16} strokeWidth={isActive ? 2.5 : 2} />}
                      {tab.label}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        </div>

        {/* Content Area (Grid) with High-End Layout Space */}
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={activeTab}
            variants={containerVariants}
            initial="hidden"
            animate="show"
            exit="exit"
            className="min-h-[500px]"
          >
            {/* 1. COLLECTIONS */}
            {activeTab === "collections" && (
              isLoadingCollections ? (
                <LoadingSkeleton />
              ) : (
                <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-x-8 md:gap-y-12">
                  {/* Create New Collection Card - Always visible in the collections grid */}
                  <motion.div variants={itemVariants} initial="hidden" animate="show" className="h-full" layout>
                    <button
                      onClick={openCreateModal}
                      className="w-full flex-col items-center justify-center gap-4 text-blue-500/60 hover:text-blue-600 transition-all cursor-pointer group bg-transparent text-left"
                    >
                      <div className="w-full aspect-[4/3] rounded-3xl border-2 border-dashed border-blue-200 bg-blue-50/30 flex items-center justify-center mb-4 transition-all duration-300 group-hover:bg-blue-50/60 group-hover:border-blue-400 group-hover:shadow-[0_8px_30px_rgba(59,130,246,0.1)] group-active:scale-[0.98]">
                        <div className="w-14 h-14 rounded-full flex items-center justify-center bg-white shadow-[0_4px_12px_rgba(0,0,0,0.05)] text-blue-500 transition-transform duration-300 group-hover:scale-110">
                          <Plus size={26} strokeWidth={2.5} />
                        </div>
                      </div>
                      <h3 className="font-bold text-slate-800 text-lg tracking-tight px-1 group-hover:text-blue-600 transition-colors">Tạo bộ sưu tập mới</h3>
                      <p className="text-slate-400 font-medium px-1 text-sm mt-1">Lưu trữ địa điểm tùy thích</p>
                    </button>
                  </motion.div>

                  {/* Filtered Collections List */}
                  {filteredCollections.map((collection) => (
                    <motion.div key={collection.id} variants={itemVariants} initial="hidden" animate="show" layout>
                      <CollectionCard
                        data={{
                          ...collection,
                          isMenuOpen: activeMenuId === collection.id,
                        }}
                        onEdit={() => openEditModal(collection)}
                        onDelete={() => {
                          setDeleteId(collection.id);
                          setIsDeleteModalOpen(true);
                          setActiveMenuId(null);
                        }}
                        onClick={() => navigate(`/saved/${collection.id}`)}
                        onMenuClick={(e) => {
                          e.stopPropagation();
                          setActiveMenuId((prev) =>
                            prev === collection.id ? null : collection.id,
                          );
                        }}
                      />
                    </motion.div>
                  ))}

                  {/* Search No Results State inside Grid */}
                  {filteredCollections.length === 0 && searchQuery !== "" && (
                    <div className="col-span-full py-16 text-center text-slate-500 text-sm font-semibold">
                      Không tìm thấy bộ sưu tập nào khớp với " {searchQuery} "
                    </div>
                  )}
                </div>
              )
            )}

            {/* 2. BLOGS */}
            {activeTab === "blogs" && (
              isLoadingBlogs ? (
                <LoadingSkeleton />
              ) : filteredBlogs.length > 0 ? (
                <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-x-8 md:gap-y-12">
                  {filteredBlogs.map((blog) => (
                    <motion.div key={blog.id} variants={itemVariants} initial="hidden" animate="show" layout>
                      <SavedBlogCard
                        blog={blog}
                        onUnbookmark={handleUnbookmarkBlog}
                      />
                    </motion.div>
                  ))}
                </div>
              ) : (
                <motion.div variants={itemVariants}>
                  <EmptyState
                    icon={<Bookmark size={36} className="mx-auto text-blue-500 mb-2" strokeWidth={1.5} />}
                    title="Chưa có bài viết nào"
                    description="Lưu các bài viết hữu ích để dễ dàng tham khảo lại cho những chuyến đi trong tương lai."
                    actionLabel="Khám phá bài viết"
                    onAction={() => navigate("/inspiration?tab=blogs")}
                    type="blogs"
                  />
                </motion.div>
              )
            )}

            {/* 3. TRIPS */}
            {activeTab === "trips" && (
              isLoadingTrips ? (
                <LoadingSkeleton />
              ) : filteredTrips.length > 0 ? (
                <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4 md:gap-x-8 md:gap-y-12">
                  {filteredTrips.map((trip) => (
                    <motion.div key={trip.id} variants={itemVariants} initial="hidden" animate="show" layout>
                      <SavedTripCard
                        trip={trip}
                        onUnfavorite={handleUnfavoriteTrip}
                        onCopy={handleCopyTrip}
                      />
                    </motion.div>
                  ))}
                </div>
              ) : (
                <motion.div variants={itemVariants}>
                  <EmptyState
                    icon={<Map size={36} className="mx-auto text-blue-500 mb-2" strokeWidth={1.5} />}
                    title="Chưa lưu chuyến đi nào"
                    description="Tìm kiếm và lưu lại những lịch trình thú vị từ cộng đồng để chuẩn bị khởi hành"
                    actionLabel="Tìm chuyến đi"
                    onAction={() => navigate("/inspiration?tab=trips")}
                    type="trips"
                  />
                </motion.div>
              )
            )}
          </motion.div>
        </AnimatePresence>
      </main>

      <Suspense fallback={null}>
        <CreateCollectionModal
          isOpen={isModalOpen}
          onClose={() => setIsModalOpen(false)}
          onConfirm={handleCreateOrUpdate}
          initialData={editingCollection}
          isLoading={isCreating || isUpdating}
        />

        <ConfirmationModal
          isOpen={isDeleteModalOpen}
          onClose={() => setIsDeleteModalOpen(false)}
          onConfirm={confirmDelete}
          isLoading={isDeleting}
          title="Xóa bộ sưu tập"
          message="Bạn sẽ xóa vĩnh viễn bộ sưu tập này. Hành động này không thể hoàn tác."
          confirmText="Xóa bỏ"
        />
      </Suspense>
    </div>
  );
};

export default SavedPage;
