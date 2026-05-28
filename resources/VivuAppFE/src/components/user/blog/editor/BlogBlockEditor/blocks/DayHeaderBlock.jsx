import React, { memo } from "react";

/**
 * DayHeaderBlock
 * Immutable (mostly) identifier for the start of a day.
 * Vercel Rule: rerender-memo
 */
const DayHeaderBlock = memo(({ dayNumber, title, content, onUpdate }) => {
  return (
    <div className="py-6 px-8 bg-slate-50 border border-slate-100 rounded-xl">
      <div className="flex flex-col items-center text-center">
        <span className="text-[10px] font-black tracking-[0.2em] uppercase text-blue-500 mb-1">
          Ngày {dayNumber}
        </span>
        <input
          type="text"
          value={title || ""}
          onChange={(e) => onUpdate({ title: e.target.value })}
          placeholder="Tiêu đề ngày (không bắt buộc)..."
          className="w-full text-center bg-transparent border-none focus:ring-0 text-xl font-bold text-gray-900 placeholder:text-gray-300 mb-2"
        />
        <textarea
          value={content || ""}
          onChange={(e) => onUpdate({ content: e.target.value })}
          placeholder="Thêm mô tả hoặc cảm xúc về ngày hôm nay..."
          className="w-full max-w-2xl text-center bg-transparent border-none focus:ring-0 text-sm text-gray-600 placeholder:text-gray-400 resize-none overflow-hidden"
          rows={2}
          onInput={(e) => {
            e.target.style.height = "auto";
            e.target.style.height = e.target.scrollHeight + "px";
          }}
        />
      </div>
    </div>
  );
});

DayHeaderBlock.displayName = "DayHeaderBlock";

export default DayHeaderBlock;
