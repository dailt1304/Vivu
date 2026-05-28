import React, { useState, useMemo, useRef, useCallback, Suspense } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Clock,
  User,
  Calendar,
  Eye,
  Star,
  Heart,
  Map as MapIcon,
  ChevronDown,
} from "lucide-react";
import {
  useBlogDetail,
  useDeleteBlog,
  useLikeBlog,
  useBookmarkBlog,
  useBlogComments,
  useCreateComment,
  useDeleteComment,
  useMyBookmarks,
} from "../../../hooks/blogs/useBlogs";
import { useUser } from "../../../hooks/useUsers";
import { useAuth } from "../../../contexts/auth-context";
import toast from "../../../utils/toast";
import AppNavbar from "../../../components/layout/AppNavbar";
import CommentSection from "../../../components/user/blog/CommentSection";
import DeleteCommentDialog from "../../../components/user/blog/DeleteCommentDialog";
import BlogFloatingActions from "../../../components/user/blog/BlogFloatingActions";
import ReportBlogModal from "../../../components/user/blog/ReportBlogModal";
import DeleteBlogDialog from "../../../components/user/blog/DeleteBlogDialog";

// Blocks
import DayDivider from "../../../components/user/blog/blocks/DayDivider";
import LocationExperienceCard from "../../../components/user/blog/blocks/LocationExperienceCard";
import QuoteBlock from "../../../components/user/blog/blocks/QuoteBlock";
import PhotoBlock from "../../../components/user/blog/blocks/PhotoBlock";
import { getRandomQuote } from "../../../utils/inspirationalQuotes";

// DrawerPortal manages its own state — opening it does NOT re-render BlogDetailPage
import DrawerPortal from "../../../components/common/drawers/DrawerPortal";

const MapContainer = React.lazy(
  () => import("../../../components/common/map/MapContainer"),
);

const BlogDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user: authUser, userId } = useAuth();
  const { data: userData } = useUser(userId);

  const user = useMemo(() => {
    if (!userData) return authUser;
    return { ...authUser, ...userData };
  }, [authUser, userData]);

  const {
    data: blog,
    isLoading: blogLoading,
    error: blogError,
  } = useBlogDetail(id);

  const { data: bookmarkedBlogsResp, mutate: mutateMyBookmarks } =
    useMyBookmarks(1, 100);
  const bookmarkedBlogIds = useMemo(() => {
    return new Set(
      (bookmarkedBlogsResp?.items || []).map((b) => b.id?.toLowerCase()),
    );
  }, [bookmarkedBlogsResp]);

  const isBookmarked = useMemo(() => {
    if (!blog) return false;
    if (typeof blog.isBookmarkedByCurrentUser === "boolean") {
      return blog.isBookmarkedByCurrentUser;
    }
    if (typeof blog.isBookmarked === "boolean") {
      return blog.isBookmarked;
    }
    return bookmarkedBlogIds.has(blog.id?.toLowerCase());
  }, [blog, bookmarkedBlogIds]);

  const { trigger: deleteBlog, isMutating: isDeleting } = useDeleteBlog();
  const { trigger: likeBlog } = useLikeBlog();
  const { trigger: bookmarkBlog } = useBookmarkBlog();

  const { data: commentsData, mutate: mutateComments } = useBlogComments(id);
  const { trigger: createComment } = useCreateComment();
  const { trigger: deleteComment } = useDeleteComment();

  const drawerRef = useRef(null);
  const [commentToDelete, setCommentToDelete] = useState(null);
  const [isDeletingComment, setIsDeletingComment] = useState(false);
  const [hoveredLocId, setHoveredLocId] = useState(null);
  const [reportModalOpen, setReportModalOpen] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  // Map Filter states
  const [selectedMapDay, setSelectedMapDay] = useState("ALL");
  const [isMapDayDropdownOpen, setIsMapDayDropdownOpen] = useState(false);

  // Parse blocks (with backward compatibility wrapper)
  // Dependency specifically on blogStoryDays length/ids so we don't regenerate random quotes on Like
  const storyDaysHash = blog?.blogStoryDays?.map((s) => s.id).join(",");
  const contentBlocks = useMemo(() => {
    if (!blog?.blogStoryDays?.length) return [];

    // Create a map to fetch Location data quickly if needed
    const locationMap = {};
    if (blog.tripLocations) {
      blog.tripLocations.forEach((tl) => {
        const locId = tl.locationId || tl.location?.id;
        if (tl.location && locId) {
          locationMap[locId] = tl.location;
        }
      });
    }

    const sorted = [...blog.blogStoryDays].sort(
      (a, b) => a.displayOrder - b.displayOrder,
    );
    const processed = [];
    let lastDay = null;
    let quoteIndex = 0;
    let locationIndex = 0;

    sorted.forEach((sd) => {
      const type = sd.blockType || "location";

      if (type === "location" && sd.dayNumber !== lastDay) {
        // Inject a day header for old data that didn't have one
        processed.push({
          _isSynthetic: true,
          id: `synthetic-day-${sd.dayNumber}`,
          blockType: "day_header",
          dayNumber: sd.dayNumber,
          title: sd.title || `Ngày ${sd.dayNumber}`,
        });

        // Inject a random quote between some days for flavor
        if (lastDay !== null && quoteIndex % 2 === 0) {
          const randQuote = getRandomQuote();
          processed.push({
            _isSynthetic: true,
            id: `synthetic-quote-${sd.dayNumber}`,
            blockType: "quote",
            content: randQuote.text,
            quoteAuthor: randQuote.author,
          });
          quoteIndex++;
        }
        lastDay = sd.dayNumber;
      } else if (type === "day_header") {
        lastDay = sd.dayNumber;
      }

      // Augment location block with full location data if available from tripLocations
      let locData = null;
      // We try to match by destinationName since old data didn't have locationId
      if (type === "location") {
        if (sd.locationId && locationMap[sd.locationId]) {
          locData = locationMap[sd.locationId];
        } else if (blog.tripLocations) {
          const matching = blog.tripLocations.find(
            (tl) => tl.location?.name === sd.destinationName,
          );
          if (matching) locData = matching.location;
        }
      }

      processed.push({
        ...sd,
        blockType: type,
        _locData: locData,
        _locIndex: type === "location" ? locationIndex++ : null,
      });
    });

    return processed;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [blog?.blogStoryDays]); // Depend on the whole array to detect content changes inside blocks

  const mapLocations = useMemo(() => {
    const locs = [];
    contentBlocks.forEach((block) => {
      if (block.blockType === "location" && block._locData) {
        locs.push({
          id: block._locData.id,
          title: block.destinationName,
          category: block._locData.categoryName || block._locData?.category?.name || "Default",
          categoryIcon: block._locData?.category?.iconUrl,
          latitude: block._locData.latitude,
          longitude: block._locData.longitude,
          dayNumber: block.dayNumber, // Extract the day from block
        });
      }
    });
    return locs;
  }, [contentBlocks]);

  const filteredMapLocations = useMemo(() => {
    if (selectedMapDay === "ALL") return mapLocations;
    return mapLocations.filter((loc) => loc.dayNumber === selectedMapDay);
  }, [mapLocations, selectedMapDay]);

  const uniqueDays = useMemo(() => {
    const days = new Set();
    mapLocations.forEach((loc) => {
      if (loc.dayNumber !== undefined && loc.dayNumber !== null) {
        days.add(loc.dayNumber);
      }
    });
    return Array.from(days).sort((a, b) => a - b);
  }, [mapLocations]);

  const normalizedComments = useMemo(() => {
    if (!commentsData?.items) return [];
    return commentsData.items.map((comment) => ({
      ...comment,
      author: {
        id: comment.userId,
        name: comment.authorName || "Người dùng",
        avatar: comment.authorAvatarUrl,
      },
      replies: (comment.replies || []).map((reply) => ({
        ...reply,
        author: {
          id: reply.userId,
          name: reply.authorName || "Người dùng",
          avatar: reply.authorAvatarUrl,
        },
      })),
    }));
  }, [commentsData]);

  const handleOpenLocation = useCallback((locationData) => {
    if (!locationData) return;
    drawerRef.current?.open(locationData);
  }, []);

  const scrollToComments = () => {
    const el = document.getElementById("comments-section");
    if (el) {
      el.scrollIntoView({ behavior: "smooth" });
    }
  };

  const handleShare = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: blog.title,
          text: blog.shortDescription,
          url: window.location.href,
        });
      } catch (err) {
        console.log("Share failed", err);
      }
    } else {
      navigator.clipboard.writeText(window.location.href);
      toast.success("Đã copy link bài viết");
    }
  };

  const handleLikeBlog = async () => {
    if (!user) {
      toast.info("Vui lòng đăng nhập để thích bài viết");
      return;
    }
    const currentIsLiked = !!(blog.isLikedByCurrentUser || blog.isLiked || blog.isLike || blog.liked);
    const currentLikeCount = blog.likeCount || 0;
    const optimisticData = {
      ...blog,
      isLikedByCurrentUser: !currentIsLiked,
      isLiked: !currentIsLiked,
      isLike: !currentIsLiked,
      liked: !currentIsLiked,
      likeCount: currentIsLiked ? currentLikeCount - 1 : currentLikeCount + 1,
    };
    import("swr").then(({ mutate: globalMutate }) => {
      globalMutate(["blog-detail", id], optimisticData, false);
      globalMutate(
        (key) => Array.isArray(key) && key[0] === "public-blogs",
        undefined,
        true,
      );
    });
    try {
      await likeBlog(blog.id);
    } catch {
      toast.error("Không thể cập nhật lượt thích");
      import("swr").then(({ mutate }) => {
        mutate(["blog-detail", id]);
      });
    }
  };

  const handleBookmarkBlog = async () => {
    if (!user) {
      toast.info("Vui lòng đăng nhập để lưu bài viết");
      return;
    }
    const optimisticData = { ...blog, isBookmarkedByCurrentUser: !isBookmarked, isBookmarked: !isBookmarked };
    import("swr").then(({ mutate }) => {
      mutate(["blog-detail", id], optimisticData, false);
    });
    try {
      await bookmarkBlog(blog.id);
      toast.success(isBookmarked ? "Đã bỏ lưu bài viết" : "Đã lưu bài viết");
      if (mutateMyBookmarks) mutateMyBookmarks();
    } catch {
      toast.error("Lỗi khi cập nhật Bookmark");
      import("swr").then(({ mutate }) => {
        mutate(["blog-detail", id]);
      });
      if (mutateMyBookmarks) mutateMyBookmarks();
    }
  };

  const handleAddComment = async (commentData) => {
    if (!user) {
      toast.info("Vui lòng đăng nhập để bình luận");
      return;
    }
    try {
      await createComment({ blogId: id, content: commentData.content });
      mutateComments();
    } catch {
      toast.error("Lỗi khi gửi bình luận");
    }
  };

  const handleDeleteComment = (commentId) => {
    setCommentToDelete(commentId);
  };

  const confirmDeleteComment = async () => {
    if (!commentToDelete) return;
    setIsDeletingComment(true);
    try {
      await deleteComment({ blogId: id, commentId: commentToDelete });
      toast.success("Đã xóa bình luận");
      mutateComments();
      setCommentToDelete(null);
    } catch {
      toast.error("Lỗi khi xóa bình luận");
    } finally {
      setIsDeletingComment(false);
    }
  };

  const handleDelete = () => {
    setShowDeleteDialog(true);
  };

  const handleDeleteConfirm = async (blogId) => {
    try {
      await deleteBlog(blogId);
      setShowDeleteDialog(false);
      toast.success("Đã xóa bài viết");
      navigate("/inspiration");
    } catch {
      toast.error("Lỗi khi xóa bài viết");
    }
  };

  // Vercel Rule: js-early-exit
  if (blogLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="w-10 h-10 border-4 border-gray-200 border-t-blue-500 rounded-full animate-spin"></div>
      </div>
    );
  }

  // Vercel Rule: js-early-exit
  if (blogError || !blog) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen">
        <p className="text-xl font-medium text-gray-600 mb-4">
          Blog không được tìm thấy hoặc có lỗi xảy ra.
        </p>
        <button
          onClick={() => navigate("/inspiration")}
          className="px-6 py-2.5 bg-blue-500 text-white font-bold rounded-xl hover:bg-blue-600 transition-colors"
        >
          Quay lại
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col min-h-screen bg-stone-50 pb-24 lg:pb-0">
      <AppNavbar />

      <main className="flex-1 w-full mx-auto px-4 md:px-8 relative">


        {/* Main Content Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-5 relative items-start gap-8 lg:gap-12 w-full mx-auto mt-6">
          {/* Left Column: Hero & Editorial Content (60%) */}
          <article className="lg:col-span-3 w-full min-w-0 pb-20 flex flex-col">
            {/* Split-style Hero Section */}
            <div className="w-full relative rounded-3xl lg:rounded-[2rem] overflow-hidden mb-12 shadow-xl border border-slate-200/50">
              <div className="h-[350px] md:h-[400px] lg:h-[450px] w-full relative bg-gray-900">
                {blog.coverImageUrl ? (
                  <img
                    src={blog.coverImageUrl}
                    alt={blog.title}
                    className="absolute inset-0 w-full h-full object-cover opacity-80"
                  />
                ) : (
                  <div className="absolute inset-0 flex items-center justify-center bg-blue-900/50">
                    <span className="text-white/30 text-2xl font-black tracking-widest uppercase">
                      Vivu
                    </span>
                  </div>
                )}
                <div className="absolute inset-0 bg-gradient-to-t from-gray-900 via-gray-900/40 to-transparent" />

                <button
                  onClick={() => navigate("/inspiration")}
                  className="absolute top-6 left-6 z-30 p-3 bg-white/90 backdrop-blur-md rounded-full text-gray-900 shadow-lg hover:bg-white transition-transform hover:scale-105 border border-white/20 cursor-pointer"
                  aria-label="Quay lại"
                >
                  <ArrowLeft size={20} />
                </button>

                <div className="absolute bottom-0 left-0 right-0 p-6 md:p-8 lg:p-12">
                  <div className="w-full">
                    <div className="flex items-center gap-3 mb-4 md:mb-6">
                      <span className="px-4 py-1.5 bg-blue-500 text-white text-xs font-black uppercase tracking-[0.2em] rounded-full">
                        {blog.tags?.[0]?.name || "Trải nghiệm"}
                      </span>
                      {blog.estimatedReadingTime && (
                        <span className="px-3 py-1.5 bg-white/10 backdrop-blur-md text-white/90 text-xs font-medium rounded-full flex items-center gap-1.5 border border-white/20">
                          <Clock size={12} /> {blog.estimatedReadingTime} phút đọc
                        </span>
                      )}
                    </div>

                    <h1 className="text-3xl sm:text-4xl md:text-5xl font-serif font-medium text-white leading-[1.15] tracking-tight mb-6 break-words">
                      {blog.title}
                    </h1>

                    <div className="flex justify-between items-end flex-wrap gap-4">
                      <div className="flex items-center gap-3">
                        <div className="w-12 h-12 rounded-full overflow-hidden border-2 border-white/20">
                          <img
                            src={
                              blog.authorAvatarUrl ||
                              "https://ui-avatars.com/api/?name=Vivu"
                            }
                            alt={blog.authorName}
                            className="w-full h-full object-cover"
                          />
                        </div>
                        <div>
                          <div className="text-white font-bold text-sm">
                            {blog.authorName || "Người dùng Vivu"}
                          </div>
                          <div className="text-white/60 text-xs mt-0.5">
                            {new Date(
                              blog.publishedAt || blog.createdAt,
                            ).toLocaleDateString("vi-VN", {
                              year: "numeric",
                              month: "long",
                              day: "numeric",
                            })}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-4 text-white/80 shrink-0">
                        <div className="flex items-center gap-1.5 text-sm font-medium">
                          <Eye size={16} /> {blog.viewCount || 0}
                        </div>
                        <div className="flex items-center gap-1.5 text-sm font-medium">
                          <Heart
                            size={16}
                            className={
                              (blog.isLikedByCurrentUser || blog.isLiked) ? "fill-red-500 text-red-500" : ""
                            }
                          />{" "}
                          {blog.likeCount || 0}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            {/* Intro Content */}
            {blog.shortDescription && (
              <p className="text-xl lg:text-2xl font-serif text-gray-500 leading-relaxed italic mb-10 border-l-2 border-slate-200 pl-6 break-words">
                {blog.shortDescription}
              </p>
            )}

            {blog.content && (
              <div className="prose prose-lg prose-stone prose-a:text-blue-600 prose-a:no-underline hover:prose-a:underline max-w-none text-gray-700 leading-relaxed mb-16 break-words">
                <div dangerouslySetInnerHTML={{ __html: blog.content }}></div>
              </div>
            )}

            {/* Block Content Engine */}
            <div className="mt-8">
              {contentBlocks.map((block) => {
                switch (block.blockType) {
                  case "day_header":
                    return (
                      <DayDivider
                        key={block.id}
                        dayNumber={block.dayNumber}
                        title={block.title}
                        content={block.content}
                      />
                    );

                  case "location":
                    return (
                      <div
                        key={block.id}
                        onMouseEnter={() => {
                          if (block._locData)
                            setHoveredLocId(block._locData.id);
                        }}
                        onMouseLeave={() => setHoveredLocId(null)}
                      >
                        <LocationExperienceCard
                          destinationName={block.destinationName}
                          locationId={block.locationId || block._locData?.id}
                          address={block._locData?.address}
                          categoryName={block._locData?.categoryName || block._locData?.category?.name}
                          categoryIconUrl={block._locData?.category?.iconUrl}
                          content={block.content}
                          images={(() => {
                            const locData = block._locData;
                            const getImageUrl = (img) => {
                              if (!img) return null;
                              if (typeof img === "string") return img;
                              return img.url || img.imageUrl || img.src || null;
                            };

                            // Priority 1: Use images from locationDetail
                            if (locData?.locationDetail?.images) {
                              try {
                                const parsed = JSON.parse(
                                  locData.locationDetail.images,
                                );
                                if (Array.isArray(parsed) && parsed.length > 0) {
                                  return parsed.map(getImageUrl).filter(Boolean);
                                }
                              } catch {
                                // Ignore parsing errors
                              }
                            }
                            
                            // Priority 2: Fallback to location images array directly
                            if (locData?.images?.length > 0) {
                              return locData.images.map(getImageUrl).filter(Boolean);
                            }
                            
                            // Priority 3: Use the imageUrl saved in the block itself
                            return block.imageUrl ? [block.imageUrl] : [];
                          })()}
                          index={block._locIndex}
                          onViewMap={() => {
                            if (block._locData)
                              handleOpenLocation(block._locData);
                          }}
                        />
                      </div>
                    );

                  case "quote":
                    return (
                      <QuoteBlock
                        key={block.id}
                        content={block.content}
                        author={block.quoteAuthor}
                      />
                    );

                  case "photo":
                    return (
                      <PhotoBlock
                        key={block.id}
                        imageUrl={block.imageUrl}
                        caption={block.content}
                        altText={block.title}
                      />
                    );

                  case "rich_text":
                  case "text":
                    return (
                      <div
                        key={block.id}
                        className="prose prose-lg prose-stone max-w-none text-gray-700 leading-relaxed my-10 break-words"
                      >
                        {block.title && (
                          <h3 className="font-serif text-2xl font-bold mb-4 break-words">
                            {block.title}
                          </h3>
                        )}
                        {block.content && (
                          <div
                            dangerouslySetInnerHTML={{ __html: block.content }}
                          />
                        )}
                      </div>
                    );

                  default:
                    return null;
                }
              })}
            </div>

            {/* Admin/Owner Actions at bottom */}
            {user?.id === blog.userId && (
              <div className="mt-16 py-6 border-t border-slate-200 flex items-center gap-4 justify-end">
                <button
                  onClick={() => navigate(`/inspiration/edit/${id}`)}
                  className="px-6 py-2.5 text-sm font-bold text-gray-700 bg-white border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors cursor-pointer"
                >
                  Chỉnh sửa bài viết
                </button>
                <button
                  onClick={handleDelete}
                  disabled={isDeleting}
                  className="px-6 py-2.5 text-sm font-bold text-white bg-red-500 rounded-xl hover:bg-red-600 transition-colors disabled:opacity-50 cursor-pointer"
                >
                  {isDeleting ? "Đang xóa..." : "Xóa bài"}
                </button>
              </div>
            )}

            {/* Comments */}
            <div
              id="comments-section"
              className="mt-20 pt-16 border-t font-sans border-slate-200"
            >

              <CommentSection
                comments={normalizedComments}
                blogId={blog.id}
                onAddComment={handleAddComment}
                onDeleteComment={handleDeleteComment}
                userAvatar={user?.avatarUrl || user?.avatar}
                userName={
                  user?.fullName || user?.name || user?.email || "Người dùng"
                }
                currentUserId={userId}
              />
            </div>
          </article>

          {/* Right Column Sticky Map (40%) */}
          {mapLocations.length > 0 && (
            <aside className="hidden lg:block lg:col-span-2 sticky top-24 shrink-0 h-[calc(100vh-8rem)] w-full">
              <div className="w-full h-full rounded-[2rem] border border-slate-200/60 shadow-[0_20px_40px_-15px_rgba(0,0,0,0.05)] overflow-hidden bg-gray-50 relative pointer-events-auto">
                {/* Custom Map Day Selector Overlay */}
                <div className="absolute top-4 right-4 z-10 w-48">
                  <div className="relative">
                    <button
                      type="button"
                      onClick={() => setIsMapDayDropdownOpen(!isMapDayDropdownOpen)}
                      className="w-full flex items-center justify-between bg-white/90 backdrop-blur-md rounded-xl shadow-lg border border-gray-100/50 px-4 py-2.5 hover:bg-white hover:shadow-xl transition-all duration-200 group ring-1 ring-black/5 cursor-pointer"
                    >
                      <div className="flex items-center gap-2">
                        <Calendar size={16} className="text-blue-500" />
                        <span className="text-sm font-bold text-gray-700">
                          {selectedMapDay === "ALL"
                            ? "Tất cả các ngày"
                            : `Ngày ${selectedMapDay}`}
                        </span>
                      </div>
                      <ChevronDown
                        size={16}
                        className={`text-gray-400 transition-transform duration-300 ${isMapDayDropdownOpen ? "rotate-180 text-blue-500" : "group-hover:text-gray-600"}`}
                      />
                    </button>

                    {/* Dropdown Menu */}
                    <div
                      className={`absolute top-full right-0 mt-2 w-56 bg-white rounded-xl shadow-2xl border border-gray-100 overflow-hidden transition-all duration-300 origin-top transform ${
                        isMapDayDropdownOpen
                          ? "opacity-100 scale-100 translate-y-0"
                          : "opacity-0 scale-95 -translate-y-2 pointer-events-none"
                      }`}
                    >
                      <div className="p-2 space-y-1 max-h-[300px] overflow-y-auto">
                        <button
                          type="button"
                          onClick={() => {
                            setSelectedMapDay("ALL");
                            setIsMapDayDropdownOpen(false);
                          }}
                          className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors cursor-pointer ${
                            selectedMapDay === "ALL"
                              ? "bg-blue-50 text-blue-700"
                              : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                          }`}
                        >
                          <div className="flex items-center gap-2.5">
                            <MapIcon
                              size={16}
                              className={selectedMapDay === "ALL" ? "text-blue-500" : "text-gray-400"}
                            />
                            <span>Tất cả các ngày</span>
                          </div>
                        </button>

                        {uniqueDays.map((day) => (
                          <button
                            key={day}
                            type="button"
                            onClick={() => {
                              setSelectedMapDay(day);
                              setIsMapDayDropdownOpen(false);
                            }}
                            className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition-colors cursor-pointer ${
                              selectedMapDay === day
                                ? "bg-blue-50 text-blue-700"
                                : "text-gray-600 hover:bg-gray-50 hover:text-gray-900"
                            }`}
                          >
                            <div className="flex flex-col items-start gap-0.5">
                              <span className="font-semibold text-gray-800">Ngày {day}</span>
                            </div>
                            {selectedMapDay === day && (
                              <div className="w-1.5 h-1.5 rounded-full bg-blue-500" />
                            )}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <Suspense
                  fallback={
                    <div className="w-full h-full flex flex-col items-center justify-center text-gray-400 bg-gray-100/50">
                      <div className="w-8 h-8 border-4 border-gray-300 border-t-blue-500 rounded-full animate-spin mb-4"></div>
                      Đang tải bản đồ...
                    </div>
                  }
                >
                  <MapContainer
                    locations={filteredMapLocations}
                    hoveredItemId={hoveredLocId}
                    // Thể hiện điểm được kích hoạt bằng màu/hover state
                    scheduledItemIds={new Set(filteredMapLocations.map((l) => l.id))}
                    hideMarkerNumbers={true}
                    onMarkerClick={(loc) => {
                      const fullLoc = contentBlocks.find(
                        (b) => b._locData?.id === loc.id,
                      )?._locData;
                      if (fullLoc) handleOpenLocation(fullLoc);
                    }}
                  />
                </Suspense>
              </div>
            </aside>
          )}
        </div>
      </main>

      {/* Floating Actions (Now Bottom Center on Desktop & Bottom Full on Mobile) */}
      <BlogFloatingActions
        likeCount={blog.likeCount}
        isLiked={blog.isLikedByCurrentUser || blog.isLiked || blog.isLike || blog.liked}
        onToggleLike={handleLikeBlog}
        commentCount={blog.commentCount}
        onNavigateComments={scrollToComments}
        saveCount={blog.saveCount}
        isSaved={isBookmarked}
        onToggleSave={handleBookmarkBlog}
        onShare={handleShare}
        onReport={() => setReportModalOpen(true)}
        isAuthor={user?.id === blog.userId}
      />

      {/* Drawer — state lives inside DrawerPortal, no parent re-render */}
      <DrawerPortal ref={drawerRef} />


      <DeleteCommentDialog
        isOpen={Boolean(commentToDelete)}
        onClose={() => setCommentToDelete(null)}
        onConfirm={confirmDeleteComment}
        isDeleting={isDeletingComment}
      />

      <ReportBlogModal
        isOpen={reportModalOpen}
        onClose={() => setReportModalOpen(false)}
        blogId={blog.id}
        blogTitle={blog.title}
      />

      <DeleteBlogDialog
        isOpen={showDeleteDialog}
        blog={blog}
        onClose={() => setShowDeleteDialog(false)}
        onConfirm={handleDeleteConfirm}
        isDeleting={isDeleting}
      />
    </div>
  );
};

export default BlogDetailPage;
