import React, { memo } from "react";
import { Quote } from "lucide-react";

/**
 * QuoteBlock Editor
 * Allows entering a quote and its author.
 * Vercel Rule: rerender-memo
 */
const QuoteBlock = memo(({ content, quoteAuthor, onUpdate }) => {
  return (
    <div className="p-8 border-l-4 border-amber-400 bg-amber-50/30 rounded-r-xl">
      <Quote className="text-amber-200 mb-2" size={32} />
      <textarea
        value={content || ""}
        onChange={(e) => onUpdate({ content: e.target.value })}
        placeholder="Nhập trích dẫn truyền cảm hứng..."
        className="w-full bg-transparent border-none focus:ring-0 text-xl font-serif italic text-gray-800 placeholder:text-gray-300 resize-none"
        rows={3}
      />
      <div className="flex items-center gap-2 mt-4 px-2">
        <div className="w-4 h-px bg-amber-200" />
        <input
          type="text"
          value={quoteAuthor || ""}
          onChange={(e) => onUpdate({ quoteAuthor: e.target.value })}
          placeholder="Tên tác giả..."
          className="bg-transparent border-none focus:ring-0 text-xs font-bold uppercase tracking-widest text-gray-500 placeholder:text-gray-300"
        />
      </div>
    </div>
  );
});

QuoteBlock.displayName = "QuoteBlock";

export default QuoteBlock;
