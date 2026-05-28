import React, { memo } from "react";
import { ImageIcon, Upload } from "lucide-react";

/**
 * PhotoBlock Editor
 * Focuses on visual storytelling with a large image and a caption.
 * Vercel Rule: rerender-memo
 */
const PhotoBlock = memo(({ imageUrl, content, title, onUpdate }) => {
  return (
    <div className="flex flex-col gap-4 p-4">
      {/* Upload Zone / Preview */}
      <div className="aspect-video lg:aspect-21/9 bg-slate-50 border-2 border-dashed border-slate-200 rounded-2xl relative overflow-hidden group">
        {imageUrl ? (
          <>
            <img
              src={imageUrl}
              alt="Preview"
              className="w-full h-full object-cover"
            />
            <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
              <button className="px-4 py-2 bg-white text-slate-900 rounded-xl font-bold text-sm shadow-xl flex items-center gap-2">
                <Upload size={16} /> Thay đổi ảnh
              </button>
            </div>
          </>
        ) : (
          <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-400 gap-2">
            <div className="p-4 bg-white rounded-full shadow-sm">
              <ImageIcon size={32} />
            </div>
            <p className="font-bold text-sm">Tải lên hình ảnh khoảnh khắc</p>
            <p className="text-xs text-slate-300">
              Click để chọn hoặc kéo thả ảnh vào đây
            </p>
          </div>
        )}
        <input
          type="file"
          className="absolute inset-0 opacity-0 cursor-pointer"
          onChange={(e) => {
            // Future: implement upload here
            const file = e.target.files[0];
            if (file) {
              const url = URL.createObjectURL(file);
              onUpdate({ 
                imageUrl: url, 
                image: file 
              });
            }
          }}
        />
      </div>

      {/* Caption & Title */}
      <div className="space-y-1">
        <input
          type="text"
          value={title || ""}
          onChange={(e) => onUpdate({ title: e.target.value })}
          placeholder="Tên ảnh / Địa danh..."
          className="w-full bg-transparent border-none focus:ring-0 text-sm font-bold text-slate-700 placeholder:text-slate-300"
        />
        <input
          type="text"
          value={content || ""}
          onChange={(e) => onUpdate({ content: e.target.value })}
          placeholder="Lời tựa cho bức ảnh này..."
          className="w-full bg-transparent border-none focus:ring-0 text-xs italic text-slate-500 placeholder:text-slate-300"
        />
      </div>
    </div>
  );
});

PhotoBlock.displayName = "PhotoBlock";

export default PhotoBlock;
