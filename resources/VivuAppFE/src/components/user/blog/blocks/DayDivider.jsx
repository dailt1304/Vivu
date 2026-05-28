import React, { memo } from "react";

/**
 * Day Divider Block
 * An editorial-style separator marking the start of a new day.
 * Vercel Rule: rerender-memo
 */
const DayDivider = memo(({ dayNumber, title, content }) => {
  return (
    <div className="my-16 relative">
      <div className="absolute inset-0 flex items-center" aria-hidden="true">
        <div className="w-full border-t border-slate-200" />
      </div>

      <div className="relative flex justify-center text-sm">
        <div className="px-6 bg-stone-50 text-center flex flex-col items-center">
          <span className="text-xs font-black tracking-[0.2em] uppercase text-blue-500 mb-2">
            Ngày {dayNumber}
          </span>
          <h3 className="text-2xl lg:text-3xl font-serif font-medium text-gray-900 leading-none break-words max-w-full">
            {title || `Khoảnh Khắc Ngày ${dayNumber}`}
          </h3>
          {content && (
            <p className="mt-3 text-gray-500 max-w-sm italic break-words">{content}</p>
          )}
        </div>
      </div>
    </div>
  );
});

DayDivider.displayName = "DayDivider";

export default DayDivider;
