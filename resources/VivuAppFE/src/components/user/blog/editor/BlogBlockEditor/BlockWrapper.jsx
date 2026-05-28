import React, { memo } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Trash2, Plus } from "lucide-react";

/**
 * BlockWrapper
 * Provides common UI for all blocks: Drag handle, Delete button, and Insertion point.
 * Vercel Rule: rerender-memo
 */
const BlockWrapper = memo(
  ({ id, children, onRemove, onAddAfter, isDragDisabled = false }) => {
    const {
      attributes,
      listeners,
      setNodeRef,
      transform,
      transition,
      isDragging,
    } = useSortable({ id, disabled: isDragDisabled });

    const style = {
      transform: CSS.Transform.toString(transform),
      transition,
      zIndex: isDragging ? 50 : "auto",
      opacity: isDragging ? 0.5 : 1,
    };

    return (
      <div ref={setNodeRef} style={style} className="group relative mb-4">
        {/* Block Container */}
        <div
          className={`relative bg-white border-2 ${isDragging ? "border-blue-500 shadow-xl" : "border-transparent"} rounded-2xl transition-all duration-200`}
        >
          {/* Actions Sidebar (Top Right, visible permanently on mobile, hover on desktop) */}
          <div className="absolute top-2 right-2 flex items-center gap-1 opacity-100 lg:opacity-0 group-hover:opacity-100 transition-opacity z-20 bg-white/80 backdrop-blur-md rounded-lg p-1 shadow-xs border border-slate-100">
            {!isDragDisabled && (
              <button
                {...attributes}
                {...listeners}
                className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200 rounded-md cursor-grab active:cursor-grabbing"
                title="Kéo để sắp xếp"
              >
                <GripVertical size={16} />
              </button>
            )}

            <button
              onClick={() => onRemove(id)}
              className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-md transition-colors"
              title="Xóa nội dung này"
            >
              <Trash2 size={16} />
            </button>
          </div>

          {/* The Actual Content */}
          <div className="p-1">{children}</div>
        </div>

        {/* Add Block Menu (Triggered between blocks) */}
        <div className="h-6 relative flex items-center justify-center -my-3 z-10 group/add">
          <div className="absolute inset-x-12 h-[2px] bg-slate-200/50 scale-x-0 group-hover/add:scale-x-100 transition-transform duration-300" />
          <button
            onClick={() => onAddAfter(id)}
            className="absolute z-10 p-1.5 bg-gradient-primary rounded-full text-white shadow-md hover:scale-110 transition-all duration-200"
            title="Thêm nội dung"
          >
            <Plus size={16} strokeWidth={2.5} />
          </button>
        </div>
      </div>
    );
  },
);

BlockWrapper.displayName = "BlockWrapper";

export default BlockWrapper;
