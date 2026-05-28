import React, { memo } from "react";
import { Quote } from "lucide-react";

/**
 * Editorial Quote Block
 * Displays an inspirational quote with a serif font and gradient border
 * Vercel Rule: rerender-memo - Memoized stateless component
 */
const QuoteBlock = memo(({ content, author }) => {
  if (!content) return null;

  return (
    <div className="my-12 px-6 lg:px-12 py-6 relative group overflow-hidden">
      {/* Decorative gradient border on the left */}
      <div className="absolute left-0 top-0 bottom-0 w-1.5 bg-linear-to-b from-amber-400 to-orange-500 rounded-full" />

      {/* Subtle background glow */}
      <div className="absolute left-0 top-1/2 -translate-y-1/2 w-32 h-32 bg-amber-400/10 blur-3xl rounded-full opacity-0 group-hover:opacity-100 transition-opacity duration-700 pointer-events-none" />

      {/* Quote Icon */}
      <Quote className="text-amber-400/40 w-12 h-12 absolute -top-2 -left-2 rotate-180 -z-10" />

      <blockquote className="relative z-10">
        <p className="font-serif text-2xl lg:text-3xl italic text-gray-800 leading-relaxed tracking-tight">
          "{content}"
        </p>

        {author && (
          <footer className="mt-4 flex items-center gap-3">
            <div className="h-px w-6 bg-amber-400" />
            <cite className="not-italic text-sm font-bold tracking-wider text-gray-400 uppercase">
              {author}
            </cite>
          </footer>
        )}
      </blockquote>
    </div>
  );
});

QuoteBlock.displayName = "QuoteBlock";

export default QuoteBlock;
