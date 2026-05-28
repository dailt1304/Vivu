import React, { memo } from "react";
import BlogEditor from "../../BlogEditor";

/**
 * RichTextBlock Editor
 * A full-featured TipTap editor block for long-form text or custom layouts.
 * Vercel Rule: rerender-memo
 */
const RichTextBlock = memo(({ title, content, onUpdate }) => {
  return (
    <div className="p-2">
      <input
        type="text"
        value={title || ""}
        onChange={(e) => onUpdate({ title: e.target.value })}
        placeholder="Tiêu đề đoạn văn (tùy chọn)..."
        className="w-full mb-4 px-2 bg-transparent border-none focus:ring-0 text-2xl font-serif font-bold text-slate-800 placeholder:text-slate-200"
      />
      <BlogEditor
        content={content}
        onChange={(html) => onUpdate({ content: html })}
        placeholder="Bắt đầu viết kỷ niệm sâu sắc của bạn ở đây..."
      />
    </div>
  );
});

RichTextBlock.displayName = "RichTextBlock";

export default RichTextBlock;
