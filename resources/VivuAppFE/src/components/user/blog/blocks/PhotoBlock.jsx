import React, { memo } from "react";

/**
 * Photo Block
 * Displays a full-width image with an optional caption.
 * Vercel Rule: rerender-memo
 */
const PhotoBlock = memo(({ imageUrl, caption, altText }) => {
  if (!imageUrl) return null;

  return (
    <figure className="my-10 w-full group">
      <div className="relative rounded-2xl overflow-hidden bg-gray-100 aspect-video lg:aspect-21/9 w-full">
        <img
          src={imageUrl}
          alt={altText || caption || "Blog image"}
          className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-[1.03]"
          loading="lazy"
        />
        {/* Subtle inner shadow for premium feel */}
        <div className="absolute inset-0 ring-1 ring-inset ring-black/5 rounded-2xl transition-opacity duration-300 pointer-events-none" />
      </div>

      {caption && (
        <figcaption className="mt-3 text-center text-sm font-medium text-gray-500 italic max-w-xl mx-auto">
          {caption}
        </figcaption>
      )}
    </figure>
  );
});

PhotoBlock.displayName = "PhotoBlock";

export default PhotoBlock;
