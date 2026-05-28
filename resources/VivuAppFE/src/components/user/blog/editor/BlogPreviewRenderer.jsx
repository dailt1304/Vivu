import React, { useMemo } from "react";
import { Clock, Eye, Heart, Star } from "lucide-react";

import DayDivider from "../blocks/DayDivider";
import LocationExperienceCard from "../blocks/LocationExperienceCard";
import QuoteBlock from "../blocks/QuoteBlock";
import PhotoBlock from "../blocks/PhotoBlock";


/**
 * BlogPreviewRenderer
 * Renders the blog content exactly as it would appear on the public detail page.
 * Uses local state (drafts) instead of fetched API data.
 */
const BlogPreviewRenderer = ({
  title,
  shortDescription,
  coverImage,
  category,
  storyDays,
  user,
}) => {

  // Handle local File blob for cover image if uploaded but not saved
  const coverImageUrl = useMemo(() => {
    if (!coverImage) return null;
    if (typeof coverImage === "string") return coverImage;
    if (coverImage instanceof File) return URL.createObjectURL(coverImage);
    return null;
  }, [coverImage]);

  // Transform storyDays into the exact format BlogDetailPage expects
  const contentBlocks = useMemo(() => {
    if (!storyDays || !storyDays.length) return [];

    // Sort just in case, though editor state usually maintains order natively
    const sorted = [...storyDays].sort(
      (a, b) => (a.displayOrder || 0) - (b.displayOrder || 0),
    );

    let locationIndex = 0;

    return sorted.map((sd) => {
      return {
        ...sd,
        blockType: sd.blockType || "location",
        _locIndex:
          sd.blockType === "location" || !sd.blockType ? locationIndex++ : null,
      };
    });
  }, [storyDays]);

  return (
    <div className="w-full bg-stone-50 rounded-[2.5rem] border border-gray-200 overflow-hidden shadow-sm">
      <main className="w-full max-w-[1200px] mx-auto px-4 md:px-8 relative pb-20">
        {/* Ad-hoc Header for Preview Mode */}
        <div className="w-full text-center py-4 mb-2 text-sm font-bold text-blue-500 tracking-widest uppercase flex items-center justify-center gap-2">
          <Eye size={16} /> Chế độ xem trước
        </div>

        {/* Hero Section (Copied from BlogDetailPage) */}
        <div className="w-full relative rounded-t-[2.5rem] md:rounded-[2.5rem] overflow-hidden mb-12 shadow-[0_20px_60px_-15px_rgba(0,0,0,0.1)]">
          <div className="aspect-video md:aspect-21/9 w-full relative bg-gray-900 overflow-hidden">
            {coverImageUrl ? (
              <img
                src={coverImageUrl}
                alt={title || "Cover"}
                className="absolute inset-0 w-full h-full object-cover transform hover:scale-105 transition-transform duration-[2s] ease-out opacity-80"
              />
            ) : (
              <div className="absolute inset-0 flex items-center justify-center bg-blue-900/50">
                <span className="text-white/30 text-2xl font-black tracking-widest uppercase">
                  Vivu
                </span>
              </div>
            )}
            <div className="absolute inset-0 bg-linear-to-t from-gray-900 via-gray-900/40 to-transparent" />

            <div className="absolute bottom-0 left-0 right-0 p-8 md:p-16 pointer-events-none">
              <div className="max-w-[780px] mx-auto">
                <div className="flex items-center gap-3 mb-6">
                  <span className="px-4 py-1.5 bg-blue-500 text-white text-xs font-black uppercase tracking-[0.2em] rounded-full shadow-lg shadow-blue-500/30">
                    {category || "Danh mục"}
                  </span>
                  <span className="px-3 py-1.5 bg-white/10 backdrop-blur-md text-white/90 text-xs font-medium rounded-full flex items-center gap-1.5 border border-white/20">
                    <Clock size={12} /> -- phút đọc
                  </span>
                </div>

                <h1 className="text-3xl sm:text-4xl md:text-5xl lg:text-6xl font-serif font-medium text-white leading-[1.15] tracking-tight mb-6 md:mb-8">
                  {title || "Tiêu đề bài viết..."}
                </h1>

                <div className="flex flex-wrap items-center gap-6 md:gap-8">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full overflow-hidden border-2 border-white/20 shadow-md">
                      <img
                        src={
                          user?.avatarUrl ||
                          user?.avatar ||
                          "https://ui-avatars.com/api/?name=Vivu"
                        }
                        alt="Author"
                        className="w-full h-full object-cover"
                      />
                    </div>
                    <div>
                      <div className="text-white font-bold text-sm">
                        {user?.fullName || user?.name || "Tác giả"}
                      </div>
                      <div className="text-white/60 text-xs mt-0.5">
                        Vừa xong
                      </div>
                    </div>
                  </div>

                  {/* Stats Mini (Mocked) */}
                  <div className="flex items-center gap-5 text-white/70">
                    <div className="flex items-center gap-1.5 text-sm font-medium">
                      <Eye size={16} /> 0
                    </div>
                    <div className="flex items-center gap-1.5 text-sm font-medium">
                      <Heart size={16} /> 0
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Main Content Layout */}
        <div className="flex relative items-start gap-8 lg:gap-12 w-full max-w-[1300px] mx-auto">
          {/* Note: Left Floating Actions are purposely omitted in Preview Mode */}

          {/* Center Editorial Content */}
          <article className="flex-1 w-full max-w-5xl min-w-0 mx-auto">
            {/* Intro Content */}
            {shortDescription ? (
              <p className="text-xl lg:text-2xl font-serif text-gray-500 leading-relaxed italic mb-10 border-l-2 border-slate-200 pl-6">
                {shortDescription}
              </p>
            ) : (
              <p className="text-xl lg:text-2xl font-serif text-gray-300 leading-relaxed italic mb-10 border-l-2 border-slate-100 pl-6">
                Mô tả ngắn sẽ hiển thị ở đây...
              </p>
            )}

            {/* Block Engine Renderer */}
            <div className="mt-8">
              {contentBlocks.length === 0 ? (
                <div className="w-full py-20 flex flex-col items-center justify-center text-gray-400 border-2 border-dashed border-gray-200 rounded-3xl">
                  <p>Nội dung bài viết chưa có khối nào.</p>
                </div>
              ) : (
                contentBlocks.map((block) => {
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
                        <div key={block.id}>
                          <LocationExperienceCard
                            destinationName={block.destinationName}
                            // Mocking LocationData heavily so it renders without crashing
                            locationId={block.locationId || "preview-loc"}
                            content={block.content}
                            images={(() => {
                              const getImageUrl = (img) => {
                                if (!img) return null;
                                if (typeof img === "string") return img;
                                return img.url || img.imageUrl || img.src || null;
                              };

                              // Priority 1: Support for trip sync data if locationDetail is present
                              if (block._locData?.locationDetail?.images) {
                                try {
                                  const parsed = JSON.parse(
                                    block._locData.locationDetail.images,
                                  );
                                  if (Array.isArray(parsed) && parsed.length > 0) {
                                    return parsed.map(getImageUrl).filter(Boolean);
                                  }
                                } catch {
                                  // ignored
                                }
                              }
                              
                              // Priority 2: Handling both draft state (array natively passed from Provider)
                              if (Array.isArray(block.images) && block.images.length > 0) {
                                return block.images.map(getImageUrl).filter(Boolean);
                              }

                              // Priority 3: Handling draft state (if images is stringified in block)
                              if (typeof block.images === "string") {
                                try {
                                  const parsed = JSON.parse(block.images);
                                  if (Array.isArray(parsed) && parsed.length > 0) {
                                    return parsed.map(getImageUrl).filter(Boolean);
                                  }
                                } catch {
                                  // If not json, maybe it's just a direct url string
                                  return [block.images];
                                }
                              }
                              
                              // Priority 4: Fallback to imageUrl in the block
                              return block.imageUrl ? [block.imageUrl] : [];
                            })()}
                            index={block._locIndex}
                            onViewMap={() => {}} // Disabled in preview
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
                          className="prose prose-lg prose-stone max-w-none text-gray-700 leading-relaxed my-10"
                        >
                          {block.title && (
                            <h3 className="font-serif text-2xl font-bold mb-4">
                              {block.title}
                            </h3>
                          )}
                          {block.content && (
                            <div
                              dangerouslySetInnerHTML={{
                                __html: block.content,
                              }}
                            />
                          )}
                        </div>
                      );

                    default:
                      return null;
                  }
                })
              )}
            </div>

            {/* Mocked Ratings Box */}
            <div className="mt-20 pt-16 border-t font-sans border-slate-200 pointer-events-none opacity-60">
              <div className="flex items-center justify-between p-6 bg-white rounded-3xl border border-slate-100 shadow-sm">
                <div className="flex items-center gap-5">
                  <div className="flex items-center gap-2">
                    <Star size={32} className="text-gray-300 fill-gray-300" />
                    <span className="text-4xl font-black text-gray-400 tracking-tight">
                      0.0
                    </span>
                  </div>
                  <div>
                    <div className="text-sm font-bold text-gray-500 uppercase tracking-widest">
                      Đánh giá chung
                    </div>
                    <div className="text-xs font-medium text-gray-400 mt-1">
                      Chưa có đánh giá nào
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </article>

          {/* Right Column Sticky Map */}
        </div>
      </main>
    </div>
  );
};

export default BlogPreviewRenderer;
