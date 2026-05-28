import { useState, useEffect, useCallback, useRef } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Save,
  Send,
  Eye,
  CheckCircle,
  AlertCircle,
} from "lucide-react";
// eslint-disable-next-line no-unused-vars
import { motion, AnimatePresence } from "framer-motion";
import { mutate } from "swr";
import AppNavbar from "../../../components/layout/AppNavbar";
import CoverImageUpload from "../../../components/user/blog/editor/CoverImageUpload";
import BlogBlockEditor from "../../../components/user/blog/editor/BlogBlockEditor";
import BlogPreviewRenderer from "../../../components/user/blog/editor/BlogPreviewRenderer";
import TripSelectWithWarning from "./TripSelectWithWarning";
import useTripDetails from "../../../hooks/trips/useTripDetails";
import {
  useCreateBlog,
  usePublishBlog,
  useUpdateBlog,
  useBlogDetail,
} from "../../../hooks/blogs/useBlogs";
import toast from "../../../utils/toast";
import { useAuth } from "../../../contexts/auth-context";
import { useCompletedTrips } from "../../../hooks/trips/useTrips";

const CATEGORIES = [
  { id: "Review", label: "Review" },
  { id: "Kinh nghiệm", label: "Kinh nghiệm" },
  { id: "Ẩm thực", label: "Ẩm thực" },
  { id: "Khám phá", label: "Khám phá" },
  { id: "Văn hóa", label: "Văn hóa" },
];

/**
 * Helper component to sync trip data into the BlockEditor
 * Since generateFromTrip is provided by useBlogBlockEditor context
 */
const EditorTripSync = ({ tripId, isEditMode }) => {
  const { generateFromTrip, blocks } = BlogBlockEditor.useEditor();
  const { trip } = useTripDetails(tripId);

  useEffect(() => {
    // Only auto-generate if we have a trip and no blocks yet (prevent overwriting on edit or reload)
    // And only in CREATE mode (not EditMode which loads from blog detail)
    if (!isEditMode && trip && blocks.length === 0) {
      generateFromTrip(trip);
    }
  }, [trip, generateFromTrip, blocks.length, isEditMode]);

  return null;
};

const CreateBlogPage = () => {
  const navigate = useNavigate();
  // Using params to support draft edit. In a real draft flow, we might change URL when draft created.
  const { id: urlBlogId } = useParams(); 
  
  // Use state for blogId so we can transition from Create -> Edit seamlessly
  const [currentBlogId, setCurrentBlogId] = useState(urlBlogId);
  const isEditMode = !!currentBlogId;
  
  const { userId, user } = useAuth();
  const [title, setTitle] = useState("");
  const [shortDescription, setShortDescription] = useState("");
  const [storyDays, setStoryDays] = useState([]);
  const [coverImage, setCoverImage] = useState(null);
  const [category, setCategory] = useState("");
  const [selectedTripId, setSelectedTripId] = useState("");
  const [isPreview, setIsPreview] = useState(false);
  
  const [lastSaved, setLastSaved] = useState(null);
  const [isSavingDraft, setIsSavingDraft] = useState(false);
  const [editLoaded, setEditLoaded] = useState(false);

  // Vercel Rule: rerender-use-ref-transient-values
  const formStateRef = useRef({ title, shortDescription, category, coverImage, storyDays });
  useEffect(() => {
    formStateRef.current = { title, shortDescription, category, coverImage, storyDays };
  }, [title, shortDescription, category, coverImage, storyDays]);

  // Ref to always have latest blogId (avoids stale closure in handleSaveDraft)
  const blogIdRef = useRef(currentBlogId);
  useEffect(() => {
    blogIdRef.current = currentBlogId;
  }, [currentBlogId]);

  // APIs
  const { data: myTripsRaw } = useCompletedTrips(userId, 1, 50);
  const myCompletedTrips = myTripsRaw?.data?.items || myTripsRaw?.items || [];

  const { trigger: createBlog } = useCreateBlog();
  const { trigger: publishBlog } = usePublishBlog();
  const { trigger: updateBlog } = useUpdateBlog();

  // Fetch existing blog data in edit mode
  const { data: existingBlog } = useBlogDetail(currentBlogId);

  // Pre-fill fields when existing blog loads (only once)
  useEffect(() => {
    if (isEditMode && existingBlog && !editLoaded) {
      setTitle(existingBlog.title || "");
      setShortDescription(existingBlog.shortDescription || "");
      const locationMap = {};
      const tripLocsMap = {};
      if (existingBlog.tripLocations) {
        existingBlog.tripLocations.forEach((tl) => {
          const locId = tl.locationId || tl.location?.id;
          if (tl.location && locId) {
            locationMap[locId] = tl.location;
            tripLocsMap[locId] = tl;
          }
        });
      }

      setStoryDays(
        (existingBlog.blogStoryDays || []).map((sd) => {
          let locData = null;
          let tlData = null;
          if (sd.blockType === "location" || !sd.blockType) {
            if (sd.locationId && locationMap[sd.locationId]) {
              locData = locationMap[sd.locationId];
              tlData = tripLocsMap[sd.locationId];
            } else if (existingBlog.tripLocations) {
              const matching = existingBlog.tripLocations.find(
                (t) => t.location?.name === sd.destinationName
              );
              if (matching) {
                locData = matching.location;
                tlData = matching;
              }
            }
          }

          let images = [];
          if (locData) {
            try {
              let parsedImages = [];
              if (locData.locationDetail?.images) {
                parsedImages = JSON.parse(locData.locationDetail.images);
              } else if (typeof locData.images === "string") {
                parsedImages = JSON.parse(locData.images);
              } else if (Array.isArray(locData.images)) {
                parsedImages = locData.images;
              }
              images = parsedImages.map(img => img?.url || img?.imageUrl || img?.src || img).filter(Boolean);
            } catch (e) {
              // ignore
            }
          }
          const firstImage = images.length > 0 ? images[0] : sd.imageUrl || "";

          let timeRange = null;
          if (tlData?.startTime && tlData?.endTime) {
            timeRange = `${tlData.startTime.substring(0, 5)} - ${tlData.endTime.substring(0, 5)}`;
          }

          return {
            id: sd.id,
            dayNumber: sd.dayNumber,
            title: sd.title,
            content: sd.content || "",
            destinationName: sd.destinationName || locData?.name,
            displayOrder: sd.displayOrder,
            blockType: sd.blockType || "location",
            locationId: sd.locationId,
            imageUrl: firstImage,
            images: images,
            quoteAuthor: sd.quoteAuthor || "",
            rating: locData?.ratingAverage || 0,
            categoryName: locData?.category?.name || "",
            address: locData?.address || "",
            timeRange: timeRange,
          };
        }),
      );
      setCoverImage(existingBlog.coverImageUrl || null);
      setCategory(
        typeof existingBlog.tags?.[0] === "object"
          ? existingBlog.tags[0].name
          : existingBlog.tags?.[0] || "",
      );
      setSelectedTripId(existingBlog.tripId || "");
      setEditLoaded(true);
    }
  }, [isEditMode, existingBlog, editLoaded]);

  const isValid =
    selectedTripId &&
    title.trim().length >= 10 &&
    shortDescription.trim().length >= 10 &&
    category;

  const dataURLtoFile = (dataurl, filename) => {
    const arr = dataurl.split(",");
    const mime = arr[0].match(/:(.*?);/)[1];
    const bstr = atob(arr[1]);
    let n = bstr.length;
    const u8arr = new Uint8Array(n);
    while (n--) {
      u8arr[n] = bstr.charCodeAt(n);
    }
    return new File([u8arr], filename, { type: mime });
  };

  const buildFormData = useCallback(() => {
    // Vercel Rule: advanced-use-latest
    const { title, shortDescription, category, coverImage, storyDays } = formStateRef.current;
    
    const formData = new FormData();
    formData.append("Title", title.trim() || "Bản nháp không tên");
    formData.append("ShortDescription", shortDescription.trim());
    if (category) formData.append("TagNames", category);

    if (coverImage) {
      if (coverImage instanceof File) {
        formData.append("CoverImage", coverImage);
      } else if (
        typeof coverImage === "string" &&
        coverImage.startsWith("data:image")
      ) {
        const file = dataURLtoFile(coverImage, "cover_image.jpg");
        formData.append("CoverImage", file);
      }
    }

    storyDays.forEach((sd, i) => {
      if (sd.id) formData.append(`StoryDays[${i}].Id`, sd.id);
      formData.append(`StoryDays[${i}].BlockType`, sd.blockType || "location");
      formData.append(`StoryDays[${i}].DayNumber`, sd.dayNumber || 0);
      formData.append(`StoryDays[${i}].Title`, sd.title || "");
      formData.append(`StoryDays[${i}].Content`, sd.content || "");
      formData.append(
        `StoryDays[${i}].DestinationName`,
        sd.destinationName || "",
      );
      if (sd.locationId) formData.append(`StoryDays[${i}].LocationId`, sd.locationId);
      if (sd.imageUrl) formData.append(`StoryDays[${i}].ImageUrl`, sd.imageUrl);
      if (sd.quoteAuthor) formData.append(`StoryDays[${i}].QuoteAuthor`, sd.quoteAuthor);
      if (sd.image instanceof File) {
        formData.append(`StoryDays[${i}].Image`, sd.image);
      }
      formData.append(`StoryDays[${i}].DisplayOrder`, sd.displayOrder || i);
    });

    return formData;
  }, []);

  const handleSaveDraft = useCallback(
    async (silent = false) => {
      // Vercel Rule: rerender-defer-reads
      const { title, shortDescription } = formStateRef.current;

      if (!title.trim() && !shortDescription.trim()) {
        if (!silent) toast.warning("Vui lòng nhập tiêu đề hoặc mô tả trước khi lưu.");
        return;
      }
      if (!selectedTripId) {
        if (!silent) toast.warning("Vui lòng chọn chuyến đi trước khi lưu nháp.");
        return;
      }

      try {
        if (!silent) setIsSavingDraft(true);
        const formData = buildFormData();

        // Use ref to always read the latest blogId (avoids stale closure)
        const activeBlogId = blogIdRef.current;
        if (activeBlogId) {
          await updateBlog({ blogId: activeBlogId, formData });
        } else {
          formData.append("TripId", selectedTripId);
          const newBlog = await createBlog(formData);
          const newId = newBlog.id;
          setCurrentBlogId(newId);
          blogIdRef.current = newId; // Update ref immediately (sync)
          window.history.replaceState(null, "", `/inspiration/edit/${newId}`);
        }

        setLastSaved(new Date());
        mutate("my-drafts");
        mutate((key) => Array.isArray(key) && key[0] === "my-blogs");
        
        if (!silent) toast.info("Đã lưu bản nháp thành công!");
      } catch (error) {
        if (!silent) toast.error("Có lỗi xảy ra khi lưu nháp.");
      } finally {
        if (!silent) setIsSavingDraft(false);
      }
    },
    [selectedTripId, buildFormData, updateBlog, createBlog]
  );

  // Auto-save draft every 60 seconds using ref to avoid closure staleness
  const saveDraftRef = useRef(handleSaveDraft);
  useEffect(() => {
    saveDraftRef.current = handleSaveDraft;
  }, [handleSaveDraft]);

  useEffect(() => {
    const interval = setInterval(() => {
      saveDraftRef.current(true); // Silent save
    }, 60000);
    return () => clearInterval(interval);
  }, []);

  const handlePublish = async () => {
    if (!isValid) return;
    try {
      const formData = buildFormData();

      if (currentBlogId) {
        await updateBlog({ blogId: currentBlogId, formData });
        
        // Only attempt to publish if it might not be published
        if (existingBlog?.status !== "published" && existingBlog?.status !== "Published") {
          try {
            await publishBlog(currentBlogId);
          } catch (publishErr) {
            // Ignore if the backend indicates it's already published
            if (publishErr?.response?.data?.code !== "Blog.AlreadyPublished") {
              throw publishErr;
            }
          }
        }
        
        toast.success("Bài viết đã được cập nhật thành công!");
      } else {
        formData.append("TripId", selectedTripId);
        const newBlog = await createBlog(formData);
        await publishBlog(newBlog.id);
        toast.success("Bài viết đã được đăng thành công!");
      }
      
      mutate("my-drafts");
      mutate((key) => Array.isArray(key) && key[0] === "my-blogs");
      navigate("/inspiration/me");
    } catch (err) {
      console.error(err);
      toast.error(
        err?.response?.data?.message || "Đã có lỗi xảy ra khi xử lý bài viết."
      );
    }
  };

  return (
    <div className="flex flex-col min-h-screen bg-gray-50">
      <AppNavbar />

      <main className="flex-1 w-full max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
          <button
            onClick={() =>
              navigate(isEditMode ? "/inspiration/me" : "/inspiration")
            }
            className="flex items-center gap-2 text-gray-600 hover:text-gray-900 font-medium transition-colors w-fit"
          >
            <ArrowLeft size={20} />
            <span>Quay lại</span>
          </button>

          <div className="flex flex-wrap items-center gap-2 sm:gap-3">
            {lastSaved && (
              <span className="hidden md:flex text-xs text-gray-400 items-center gap-1 mr-2 border-r border-gray-200 pr-3">
                <CheckCircle size={12} />
                Đã lưu {lastSaved.toLocaleTimeString("vi-VN")}
              </span>
            )}
            <button
              onClick={() => setIsPreview(!isPreview)}
              className={`flex items-center gap-2 px-3 sm:px-4 py-2 rounded-xl font-medium transition-all text-sm sm:text-base ${
                isPreview
                  ? "bg-blue-100 text-blue-700"
                  : "bg-gray-100 text-gray-600 hover:bg-gray-200"
              }`}
            >
              <Eye size={18} />
              <span className={isPreview ? "" : "hidden mini:inline"}>
                {isPreview ? "Thoát xem trước" : "Xem trước"}
              </span>
            </button>
            <button
              onClick={() => handleSaveDraft()}
              disabled={isSavingDraft || (!title && !shortDescription && !selectedTripId)}
              className="flex items-center gap-2 px-3 sm:px-4 py-2 bg-gray-100 text-gray-600 rounded-xl font-medium hover:bg-gray-200 transition-colors disabled:opacity-50 text-sm sm:text-base"
            >
              <Save size={18} className={isSavingDraft ? "animate-pulse" : ""} />
              <span className="hidden mini:inline">
                {isSavingDraft ? "Đang lưu..." : "Lưu nháp"}
              </span>
            </button>
            <button
              onClick={handlePublish}
              disabled={!isValid}
              className="flex items-center gap-2 px-4 sm:px-5 py-2 bg-gradient-primary text-white font-bold rounded-xl shadow-lg shadow-blue-200 hover:shadow-xl hover:scale-105 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 text-sm sm:text-base"
            >
              <Send size={18} />
              <span>
                {existingBlog?.status === "published" || existingBlog?.status === "Published"
                  ? "Lưu thay đổi"
                  : isEditMode
                    ? "Cập nhật & Đăng"
                    : "Đăng bài"}
              </span>
            </button>
          </div>
        </div>

        {/* Content */}
        <AnimatePresence mode="wait">
          {isPreview ? (
            <motion.div
              key="preview"
              initial={{ opacity: 0, scale: 0.98, y: 10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.98, y: -10 }}
              transition={{ type: "spring", stiffness: 100, damping: 20 }}
            >
              <BlogPreviewRenderer
                title={title || "Bản nháp không tên"}
                shortDescription={shortDescription || "Chưa có mô tả"}
                coverImage={coverImage}
                category={
                  CATEGORIES.find((c) => c.id === category)?.label || category
                }
                storyDays={storyDays}
                user={user}
              />
            </motion.div>
          ) : (
            <motion.div
              key="editor"
              initial={{ opacity: 0, scale: 0.98, y: -10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.98, y: 10 }}
              transition={{ type: "spring", stiffness: 100, damping: 20 }}
              className="space-y-6"
            >
              {/* Cover Image */}
              <CoverImageUpload value={coverImage} onChange={setCoverImage} />

              {/* Title */}
              <div className="relative mb-2">
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Tiêu đề bài viết..."
                  className="w-full text-3xl font-bold text-gray-900 placeholder:text-gray-400 bg-transparent border-none focus:outline-none"
                />
                <div
                  className={`text-sm flex items-center gap-1 font-medium transition-colors mt-2 ${
                    title.trim().length >= 10
                      ? "text-green-600"
                      : title.trim().length > 0
                        ? "text-amber-500"
                        : "text-gray-400"
                  }`}
                >
                  {title.trim().length >= 10 ? (
                    <CheckCircle size={14} />
                  ) : (
                    <AlertCircle size={14} />
                  )}
                  {title.trim().length} / 10 ký tự tối thiểu
                </div>
              </div>

              {/* Short Description */}
              <div className="bg-white rounded-2xl border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="font-bold text-gray-900 flex items-center gap-2">
                    Mô tả ngắn <span className="text-red-500">*</span>
                  </h3>
                  <div
                    className={`text-sm font-medium flex items-center gap-1 transition-colors ${
                      shortDescription.trim().length >= 10
                        ? "text-green-600"
                        : shortDescription.trim().length > 0
                          ? "text-amber-500"
                          : "text-gray-400"
                    }`}
                  >
                    {shortDescription.trim().length >= 10 ? (
                      <CheckCircle size={14} />
                    ) : (
                      <AlertCircle size={14} />
                    )}
                    {shortDescription.trim().length} / 300
                  </div>
                </div>
                <textarea
                  value={shortDescription}
                  onChange={(e) => {
                    if (e.target.value.length <= 300) {
                      setShortDescription(e.target.value);
                    }
                  }}
                  placeholder="Giới thiệu ngắn gọn về bài viết của bạn..."
                  rows={3}
                  className="w-full resize-none border border-gray-200 rounded-xl px-4 py-3 text-gray-700 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500/30 focus:border-blue-400 transition-all"
                />
              </div>

              {/* Category Selection */}
              <div className="bg-white rounded-2xl border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="font-bold text-gray-900 flex items-center gap-2">
                    Chọn thể loại bài viết{" "}
                    <span className="text-red-500">*</span>
                  </h3>
                  {category ? (
                    <span className="text-sm font-medium text-green-600 flex items-center gap-1">
                      <CheckCircle size={14} /> Đã chọn
                    </span>
                  ) : (
                    <span className="text-sm font-medium text-amber-500 flex items-center gap-1">
                      <AlertCircle size={14} /> Bắt buộc
                    </span>
                  )}
                </div>
                <div className="flex flex-wrap gap-2">
                  {CATEGORIES.map((cat) => (
                    <button
                      key={cat.id}
                      onClick={() => setCategory(cat.id)}
                      className={`px-4 py-2 rounded-full text-sm font-medium border transition-all ${
                        category === cat.id
                          ? "bg-gradient-primary text-white border-transparent"
                          : "bg-white text-gray-600 border-gray-200 hover:border-blue-300"
                      }`}
                    >
                      {cat.label}
                    </button>
                  ))}
                </div>
              </div>

              {/* Trip Selection via Modal Component */}
              <TripSelectWithWarning
                myCompletedTrips={myCompletedTrips}
                selectedTripId={selectedTripId}
                isEditMode={isEditMode}
                onConfirmChange={(newTripId) => {
                  setSelectedTripId(newTripId);
                  setStoryDays([]); // Wipe out old blocks to let EditorTripSync fetch new ones
                  toast.success("Đã cập nhật chuyến đi mới!");
                }}
              />

              {/* Blog Block Editor */}
              <div className="bg-white rounded-[2rem] border border-gray-200 overflow-hidden shadow-sm">
                <div className="bg-slate-50 px-8 py-6 border-b border-gray-100 flex items-center justify-between">
                  <div>
                    <h3 className="text-xl font-bold text-gray-900">
                      Nội dung Blog
                    </h3>
                    <p className="text-sm text-gray-500 mt-0.5">
                      Kể câu chuyện của bạn theo từng khối nội dung
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="px-3 py-1 bg-blue-100 text-blue-600 rounded-full text-[10px] font-black uppercase tracking-widest">
                      Editorial Mode
                    </div>
                  </div>
                </div>

                <BlogBlockEditor.Provider
                  key={`blog-editor-${selectedTripId || "empty"}`} // Forces remount and blocks clear when TripID changes
                  initialBlocks={storyDays}
                  onChange={setStoryDays}
                >
                  <EditorTripSync
                    tripId={selectedTripId}
                    isEditMode={isEditMode}
                  />
                  <BlogBlockEditor.List />
                </BlogBlockEditor.Provider>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </main>
    </div>
  );
};

export default CreateBlogPage;
