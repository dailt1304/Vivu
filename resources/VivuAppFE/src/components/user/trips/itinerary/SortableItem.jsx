import React from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useTripItineraryContext } from "../TripItinerary/TripItineraryContext";
import {
  Map,
  Heart,
  MapPin,
  MoreHorizontal,
  Eye,
  Trash2,
  GripVertical,
  Clock,
  Info,
  MoreVertical,
  CalendarPlus,
  StickyNote,
  DollarSign,
  Shuffle,
  CornerDownRight,
  ChevronDown,
} from "lucide-react";
import InlineNoteEditor from "./InlineNoteEditor";
import CategoryIcon from "../../../common/CategoryIcon";

const DEFAULT_IMAGE =
  "https://images.unsplash.com/photo-1501785888041-af3ef285b470?q=80&w=800&auto=format&fit=crop";

const handleImageError = (e) => {
  e.target.onerror = null; // Prevent infinite loop if default image also fails
  e.target.src = DEFAULT_IMAGE;
};

const SortableItem = ({
  id,
  item,
  type,
  onDelete,
  activeMenuId,
  onToggleMenu,
  onEditTime,
  onViewDetails,
  onAddToSchedule,
  onMoveToIdeas,
  onNoteChange,
  isOverlay,
  onItemHover,
  onItemLeave,
  onSwapLocation,
  onQuickAddToDay,
  dayOptions,
  // Direct props as fallbacks for 'item'
  content: directContent,
  image: directImage,
  category: directCategory,
}) => {
  const content = directContent || item?.content || "";
  const image = directImage || item?.image;
  const category = directCategory || item?.category;
  const startTime = item?.startTime;
  const endTime = item?.endTime;
  const initialNote = item?.note;
  const alternatives = item?.alternatives;

  const [isNoteOpen, setIsNoteOpen] = React.useState(false);
  const [noteDraft, setNoteDraft] = React.useState(initialNote || "");

  const prevNoteRef = React.useRef(initialNote);
  if (prevNoteRef.current !== initialNote) {
    prevNoteRef.current = initialNote;
    if (noteDraft !== initialNote) {
      setNoteDraft(initialNote || "");
    }
  }

  const handleNoteSave = (html) => {
    if (html !== (initialNote || "")) {
      setNoteDraft(html);
      onNoteChange?.(id, html);
    }
  };

  const handleEditTime = () => onEditTime?.(item);
  const handleViewDetails = () => onViewDetails?.(item);
  const handleMoveToIdeas = () => onMoveToIdeas?.(item);
  const handleMouseEnter = () => onItemHover?.(item?.id || id);

  const { state } = useTripItineraryContext();
  const canEdit = state.canEdit;

  const alternativeList = Array.isArray(alternatives)
    ? alternatives.slice(0, 2)
    : [];

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id, disabled: !canEdit });

  const isMenuOpen = activeMenuId === id;

  const style = {
    transform: type === "idea" ? undefined : CSS.Transform.toString(transform),
    transition: type === "idea" ? undefined : transition,
    opacity: isDragging && type !== "idea" && !isOverlay ? 0.4 : 1,
  };

  // Card Variant (Grid Idea - Full Image Style)
  if (type === "idea") {
    return (
      <div
        ref={setNodeRef}
        style={style}
        {...attributes}
        {...listeners}
        onClick={() => {
          if (handleViewDetails && !isMenuOpen) handleViewDetails();
        }}
        className={`relative aspect-square group ${canEdit ? "cursor-grab active:cursor-grabbing" : "cursor-default"} shadow-sm hover:shadow-[0_8px_30px_rgb(0,0,0,0.12)] ${!isDragging ? "transition-all duration-300" : ""} rounded-3xl ${isMenuOpen ? "z-60 ring-2 ring-blue-500 ring-offset-2" : "z-0"}`}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={onItemLeave}
      >
        {/* Inner Content - Clipped */}
        <div className="absolute inset-0 rounded-3xl overflow-hidden border border-gray-200/60">
          {/* Background Image */}
          <img
            src={image || DEFAULT_IMAGE}
            alt={content}
            className="absolute inset-0 w-full h-full object-cover"
            onError={handleImageError}
          />

          {/* Gradient Overlay */}
          <div className="absolute inset-0 bg-linear-to-t from-black/90 via-black/20 to-transparent" />

          {/* Drag Handle Indicator */}
          {canEdit && (
            <div className="absolute top-2 left-2 p-1.5 bg-black/20 backdrop-blur-md rounded-full text-white opacity-70 transition-opacity duration-300 z-10 cursor-grab"
              title="Kéo thả vào lịch trình"
            >
              <GripVertical size={14} />
            </div>
          )}

          {/* Bottom Content */}
          <div className="absolute bottom-0 left-0 right-0 p-3 z-10">
            <h4 className="text-white font-bold text-sm leading-snug mb-1 drop-shadow-md line-clamp-2">
              {content}
            </h4>
            <div className="flex items-center gap-1 text-white/90 text-[10px] font-medium">
              <CategoryIcon
                name={typeof category === "object" ? category?.name : category}
                iconUrl={
                  typeof category === "object" ? category?.iconUrl : null
                }
                size={10}
                className="text-white/80"
              />
              <span className="truncate">
                {typeof category === "object"
                  ? category?.name
                  : category || "Attraction"}
              </span>
            </div>
          </div>
        </div>

        <div className="absolute top-2 right-2 z-20">
          <button
            className="p-1.5 bg-black/20 backdrop-blur-md rounded-full text-white hover:bg-white/20 transition-colors opacity-100 duration-300"
            onPointerDown={(e) => {
              // Prevent drag start
              e.stopPropagation();
            }}
            onClick={(e) => {
              e.stopPropagation();
              onToggleMenu(id);
            }}
          >
            <MoreHorizontal size={14} />
          </button>

          {isMenuOpen && (
            <div
              className="absolute right-0 mt-2 w-48 bg-white rounded-xl shadow-xl border border-slate-100 overflow-hidden text-left animate-in fade-in zoom-in-95 duration-200 origin-top-right z-60 ring-1 ring-black/5"
              onPointerDown={(e) => e.stopPropagation()}
            >
              {/* Submenu: Thêm vào ngày nhanh */}
              {dayOptions && dayOptions.length > 0 && (
                <>
                  <div className="px-4 py-2 text-xs font-bold text-slate-400 uppercase tracking-wider border-b border-slate-50">
                    Thêm vào lịch trình
                  </div>
                  {dayOptions.map((day) => (
                    <button
                      key={day.id}
                      className="w-full flex items-center gap-3 px-4 py-2.5 text-sm font-medium text-slate-700 hover:bg-blue-50 hover:text-blue-600 transition-colors"
                      onClick={(e) => {
                        e.stopPropagation();
                        onToggleMenu(null);
                        onQuickAddToDay?.(id, day.id);
                      }}
                    >
                      <CalendarPlus size={14} className="text-slate-400" />
                      {day.label}
                    </button>
                  ))}
                </>
              )}
              {onSwapLocation && (
                <button
                  className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-amber-600 hover:bg-amber-50 hover:text-amber-700 transition-colors border-t border-slate-50"
                  onClick={(e) => {
                    e.stopPropagation();
                    onToggleMenu(null); // Close menu
                    onSwapLocation(id);
                  }}
                >
                  <Shuffle size={16} /> Thay thế địa điểm
                </button>
              )}
              <button
                className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-red-600 hover:bg-red-50 hover:text-red-700 transition-colors border-t border-slate-50"
                onClick={(e) => {
                  e.stopPropagation();
                  onToggleMenu(null); // Close menu
                  if (onDelete) onDelete(id);
                }}
              >
                <Trash2 size={16} /> Xóa
              </button>
            </div>
          )}
        </div>
      </div>
    );
  }

  // List Item Variant (Day Schedule)
  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className={`group relative flex flex-col gap-0 bg-white rounded-3xl border 
                ${!isDragging ? "transition-all duration-300" : ""}
                ${canEdit ? "cursor-grab active:cursor-grabbing" : "cursor-default"}
                ${isDragging ? "shadow-2xl shadow-blue-500/10 scale-[1.02] rotate-1 z-50 border-blue-300" : "shadow-sm hover:shadow-[0_8px_30px_rgb(0,0,0,0.06)] border-gray-100 hover:border-gray-200"}
                ${isMenuOpen ? "z-60 ring-2 ring-blue-500 border-transparent" : ""}
                ${isOverlay ? "w-auto min-w-[300px] inline-flex pr-6 ring-2 ring-blue-500/30 shadow-2xl" : ""}
                mb-4
            `}
    >
      {/* 1. Main Card Content */}
      <div className="relative z-20 flex items-center gap-3 md:gap-4 p-2.5 md:p-3 w-full">
        {/* 1.1 Drag Handle */}
        {!isOverlay && canEdit && (
          <div className="flex flex-col items-center self-stretch justify-center w-5 md:w-6 shrink-0 md:pr-1 md:mr-1 opacity-70 hover:opacity-100 transition-opacity">
            <GripVertical
              size={18}
              className="text-gray-400 group-hover:text-blue-500 transition-colors"
            />
          </div>
        )}

        {/* 1.2 Image */}
        <div className="relative w-14 h-14 md:w-16 md:h-16 shrink-0 rounded-2xl overflow-hidden shadow-inner bg-gray-100 border border-gray-50 group-hover:scale-[1.03] transition-transform duration-500">
          <img
            src={image || DEFAULT_IMAGE}
            alt={content}
            className="w-full h-full object-cover"
            onError={handleImageError}
          />
        </div>

        {/* 1.3 Content Info */}
        <div className="flex-1 min-w-0 flex flex-col justify-center gap-1">
          <div className="flex items-center gap-2">
            <h4 className="text-[15px] font-bold text-gray-900 truncate leading-tight tracking-tight group-hover:text-blue-600 transition-colors">
              {content}
            </h4>
            {noteDraft ? (
              <div
                className="flex items-center gap-1 px-1.5 py-0.5 bg-blue-50 text-blue-500 rounded-md ring-1 ring-blue-100/50"
                title="Có ghi chú"
              >
                <StickyNote size={10} />
              </div>
            ) : null}
          </div>

          <div className="flex items-center gap-2 flex-wrap mt-0.5">
            {/* Time Badge */}
            <div className="flex items-center gap-1.5 px-2 py-1 bg-gray-50 text-gray-700 rounded-lg border border-gray-100 group-hover:bg-blue-50 group-hover:text-blue-700 group-hover:border-blue-100 transition-colors">
              <Clock size={10} />
              <span className="text-[11px] font-bold font-mono tracking-tight">
                {startTime || "--:--"} - {endTime || "--:--"}
              </span>
            </div>

            {/* Category Dot */}
            <div className="flex items-center gap-1.5 text-[11px] font-semibold text-gray-400">
              <CategoryIcon
                name={typeof category === "object" ? category?.name : category}
                iconUrl={
                  typeof category === "object" ? category?.iconUrl : null
                }
                size={12}
                className="text-blue-500"
              />
              <span className="">
                {typeof category === "object"
                  ? category?.name
                  : category || "Location"}
              </span>
            </div>
          </div>
        </div>

        {/* 1.4 Actions */}
        {!isOverlay && (
          <div className="flex items-center gap-1 opacity-100 transition-all duration-200">
            <button
              className={`p-2 rounded-xl transition-all ${isNoteOpen ? "text-blue-600 bg-blue-50 shadow-inner" : "text-slate-400 hover:text-blue-600 hover:bg-blue-50"}`}
              title="Ghi chú & Kinh phí"
              onPointerDown={(e) => e.stopPropagation()}
              onClick={(e) => {
                e.stopPropagation();
                setIsNoteOpen(!isNoteOpen);
              }}
            >
              <StickyNote size={18} />
            </button>

            <button
              className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-xl transition-colors"
              title="Xem chi tiết"
              onPointerDown={(e) => e.stopPropagation()}
              onClick={(e) => {
                e.stopPropagation();
                if (handleViewDetails) handleViewDetails();
              }}
            >
              <Info size={18} />
            </button>

            <div className="relative z-[100]">
              <button
                className="p-2 text-slate-400 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition-colors"
                onPointerDown={(e) => e.stopPropagation()}
                onClick={(e) => {
                  e.stopPropagation();
                  onToggleMenu && onToggleMenu(id);
                }}
              >
                <MoreVertical size={18} />
              </button>

              {/* Menu Dropdown */}
              {isMenuOpen && (
                <div
                  className="absolute right-0 top-full mt-1 w-56 bg-white rounded-xl shadow-xl border border-slate-100 overflow-hidden text-left animate-in fade-in zoom-in-95 duration-200 origin-top-right z-60 ring-1 ring-black/5"
                  onPointerDown={(e) => e.stopPropagation()}
                >
                  <button
                    className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-blue-50 hover:text-blue-600 transition-colors"
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleMenu(null);
                      if (handleEditTime) handleEditTime();
                    }}
                  >
                    <Clock size={16} /> Sửa giờ
                  </button>
                  <button
                    className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-slate-700 hover:bg-blue-50 hover:text-blue-600 transition-colors border-t border-slate-50"
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleMenu(null);
                      setIsNoteOpen(true);
                    }}
                  >
                    <StickyNote size={16} /> Thêm ghi chú
                  </button>
                  {canEdit && (
                    <button
                      className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-blue-600 hover:bg-blue-50 transition-colors border-t border-slate-50"
                      onClick={(e) => {
                        e.stopPropagation();
                        onToggleMenu(null);
                        if (handleMoveToIdeas) handleMoveToIdeas();
                      }}
                    >
                      <CalendarPlus size={16} /> Đưa vào ý tưởng
                    </button>
                  )}

                  {/* Alternative Location Action - Allow up to 2 alternatives */}
                  {canEdit && alternativeList.length < 2 && (
                    <button
                      className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-amber-600 hover:bg-amber-50 transition-colors border-t border-slate-50"
                      onClick={(e) => {
                        e.stopPropagation();
                        onToggleMenu(null);
                        // Trigger pick alternative via custom event or context action
                        const event = new CustomEvent("pickAlternative", {
                          detail: { item },
                        });
                        window.dispatchEvent(event);
                      }}
                    >
                      <Map size={16} /> Tìm địa điểm dự phòng
                    </button>
                  )}

                  <button
                    className="w-full flex items-center gap-3 px-4 py-3 text-sm font-medium text-red-600 hover:bg-red-50 hover:text-red-700 transition-colors border-t border-gray-50"
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleMenu(null);
                      if (onDelete) onDelete(id);
                    }}
                  >
                    <Trash2 size={16} /> Xóa
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* 2. Inline Expandable Notes Section */}
      {!isOverlay ? (
        <div
          className={`grid transition-all duration-500 ease-[cubic-bezier(0.2,1,0.3,1)] ${isNoteOpen ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0 overflow-hidden"}`}
        >
          <div className="overflow-hidden min-h-0">
            <div className="px-4 pb-4 pt-1 flex flex-col gap-3">
              <div className="h-px bg-slate-100 w-full mb-1" />

              <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-bold uppercase tracking-widest">
                <StickyNote size={12} className="text-blue-400" />
                <span>Ghi chú</span>
              </div>

              <InlineNoteEditor
                initialContent={noteDraft}
                onBlur={handleNoteSave}
                isOpen={isNoteOpen}
              />
            </div>
          </div>
        </div>
      ) : null}

      {/* 3. Nested Branching Section (Alternative Plan) */}
      {!isOverlay && alternativeList.length > 0 && (
        <div className="flex flex-col gap-2">
          {alternativeList.map((alt, index) => (
            <div
              key={alt.id || index}
              className="relative mt-1 mb-1 ml-12 mr-3 md:ml-14 group/alt-container"
            >
              {/* Connector Line */}
              <div className="absolute -left-6 -top-12 bottom-1/2 w-6 border-l-2 border-b-2 border-dashed border-slate-200 rounded-bl-2xl pointer-events-none" />

              <div className="flex items-center gap-3 p-2 bg-slate-50/50 rounded-2xl border border-slate-100 hover:border-blue-100 hover:bg-blue-50/30 transition-all group/alt">
                <div className="w-10 h-10 rounded-xl overflow-hidden shrink-0 border border-white shadow-sm bg-slate-100">
                  <img
                    src={
                      (() => {
                        const parsed = JSON.parse(alt.images || "[]");
                        return (parsed[0]?.url || parsed[0]) || DEFAULT_IMAGE;
                      })()
                    }
                    alt={alt.locationName}
                    className="w-full h-full object-cover opacity-80 group-hover/alt:opacity-100 transition-opacity"
                    onError={handleImageError}
                  />
                </div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-1.5 mb-0.5">
                    <div className="px-1.5 py-0.5 bg-amber-100 text-amber-700 text-[8px] font-black uppercase tracking-wider rounded-md">
                      Dự phòng{" "}
                      {alternativeList.length > 1 ? `#${index + 1}` : ""}
                    </div>
                    <h5 className="text-[12px] font-bold text-slate-700 truncate group-hover/alt:text-slate-900">
                      {alt.locationName}
                    </h5>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex items-center gap-1 text-[10px] text-slate-400 font-medium">
                      <MapPin size={10} />
                      <span className="truncate max-w-[150px]">
                        {alt.locationAddress || "Đang cập nhật"}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-1 mr-1">
                  {canEdit && (
                    <>
                      <button
                        className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-all opacity-100"
                        title="Xóa địa điểm dự phòng"
                        onClick={(e) => {
                          e.stopPropagation();
                          const event = new CustomEvent("removeAlternative", {
                            detail: { alternativeId: alt.id },
                          });
                          window.dispatchEvent(event);
                        }}
                      >
                        <Trash2 size={12} />
                      </button>
                      <button
                        className="p-1.5 text-slate-400 hover:text-amber-600 hover:bg-amber-50 rounded-lg transition-all opacity-100 flex items-center gap-1"
                        title="Chuyển thành địa điểm chính"
                        onClick={(e) => {
                          e.stopPropagation();
                          const event = new CustomEvent("swapAlternative", {
                            detail: {
                              tripLocationId: item.tripLocationId,
                              alternativeId: alt.id,
                            },
                          });
                          window.dispatchEvent(event);
                        }}
                      >
                        <span className="text-[10px] font-bold">Sử dụng</span>
                        <Shuffle size={12} />
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default React.memo(SortableItem);
