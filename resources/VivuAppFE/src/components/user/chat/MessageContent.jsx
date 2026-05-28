import React from "react";
import { MapPin, Clock } from "lucide-react";

/**
 * Parses and renders markdown-like content in AI messages.
 * Supports: **bold**, - lists, ### headings, ---, day cards
 */
const MessageContent = ({ content }) => {
  if (!content) return null;

  // Split by lines and process
  const lines = content.split("\n");
  let currentDayCard = null;
  let currentList = [];
  const elements = [];
  let key = 0;

  const flushList = () => {
    if (currentList.length > 0) {
      elements.push(
        <div key={key++} className="ml-1 mb-4 mt-2 font-sans text-gray-800">
          {currentList.map((itemObj, idx) => {
             const { text, isIndented = false } = itemObj;
             // Match **Ngày 1**, **Gợi ý Ngày 4**, etc.
             const isDayTitle = /^\*\*\s*(?:.*?Ngày\s+\d+.*?)\s*\*\*.*$/.test(text.trim()) && !isIndented;

             if (isDayTitle) {
               return (
                 <div key={idx} className="relative z-10 flex flex-col sm:flex-row sm:items-center gap-3 mt-6 mb-3 first:mt-0 pb-1">
                    <h3 className="text-base font-extrabold text-slate-800 tracking-tight leading-snug">
                      {parseInlineMarkdown(text.replace(/^\*\*(.*?)\*\*(.*)$/, "$1$2").trim())}
                    </h3>
                 </div>
               );
             }

             // Handle secondary item, extract bold time/label if exists
             let timeLabel = null;
             let bodyText = text;
             const boldPrefixRegex = /^\*\*\s*(.*?)\s*\*\*\s*(.*)$/;
             const match = text.match(boldPrefixRegex);
             if (match) {
               timeLabel = match[1].replace(/:\s*$/, "");
               bodyText = match[2].replace(/^:\s*/, ""); // Remove orphaned colon
             }

             return (
               <div key={idx} className="relative z-0 ml-[11px] pl-6 pt-1 pb-4 border-l-[2px] border-gray-100 group last:border-transparent last:pb-0">
                  <div className="absolute -left-[2px] top-0 h-[22px] w-[2px] bg-gray-100 hidden group-last:block" />
                  
                  <div className="absolute -left-[10px] top-[14px] w-[18px] h-[18px] bg-white rounded-full flex items-center justify-center shadow-[0_0_0_4px_white] border border-blue-200">
                     <div className="w-[5px] h-[5px] bg-blue-500 rounded-full" />
                  </div>
                  
                  <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-3 -mt-2 hover:shadow-md transition-shadow relative">
                    <div className="absolute top-4 -left-[6px] w-3 h-3 bg-white border-l border-b border-gray-100 transform rotate-45"></div>
                    
                    <div className="text-gray-700 leading-relaxed text-[14px] relative z-10">
                      {timeLabel ? (
                        <div className="space-y-1">
                          <span className="font-bold text-gray-900 block text-[14.5px]">
                            {parseInlineMarkdown(timeLabel)}
                          </span>
                          <span className="block">{parseInlineMarkdown(bodyText)}</span>
                        </div>
                      ) : parseInlineMarkdown(text)}
                    </div>
                  </div>
               </div>
             );
          })}
        </div>
      );
      currentList = [];
    }
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Horizontal rule - skip (we'll use day headers instead)
    if (line.trim() === "---") {
      flushList();
      continue;
    }

    // Day Heading (### Ngày X or ### Title)
    if (line.startsWith("### ")) {
      flushList();
      // If there's a previous day card, push it
      if (currentDayCard) {
        elements.push(renderDayCard(currentDayCard, key++));
      }
      // Start new day card
      currentDayCard = {
        title: line.replace("### ", ""),
        locations: [],
      };
      continue;
    }

    // Location item (starts with number or bullet)
    // Matches: "1. Name" or "1. Name (08:00)" or "**1. Name**"
    const locationMatch = line.match(/^\*?\*?(\d+)\.\s+(.+)/);
    if (locationMatch && currentDayCard) {
      const index = locationMatch[1];
      let rawName = locationMatch[2].replace(/\*\*/g, "").trim();
      let time = "";

      // Check for time at the end (HH:mm)
      const timeMatch = rawName.match(/\(\d{2}:\d{2}\)$/);
      if (timeMatch) {
        time = timeMatch[0].replace(/[()]/g, "");
        rawName = rawName.replace(/\(\d{2}:\d{2}\)$/, "").trim();
      }

      currentDayCard.locations.push({
        index: index,
        name: rawName,
        time: time,
      });
      continue;
    }

    // If we have an open day card and hit non-location content, close the card
    if (currentDayCard && line.trim() && !line.startsWith("   ")) {
      elements.push(renderDayCard(currentDayCard, key++));
      currentDayCard = null;
    }

    if (!line.trim()) {
      // Intentionally do not flush empty lines to allow multi-line timelines to connect
      continue;
    }

    // Regular content
    if (line.trim() && !currentDayCard) {
      const bulletMatch = line.match(/^\s*[-*]\s+(.*)/);
      if (bulletMatch) {
        currentList.push({ text: bulletMatch[1], isIndented: /^\s{2,}/.test(line) });
        continue;
      }
      
      const dayHeaderRegex = line.match(/^\*\*\s*(?:.*?Ngày\s+\d+.*?)\s*\*\*.*$/);
      if (dayHeaderRegex) {
        currentList.push({ text: line.trim(), isIndented: false });
        continue;
      }

      flushList(); // Not a list item or heading, flush open list

      // Title line (starts with emoji or **)
      if (line.startsWith("✨") || line.startsWith("**✨")) {
        elements.push(
          <h2 key={key++} className="text-lg font-bold text-gray-900 mb-2">
            {parseInlineMarkdown(line.replace(/✨|\*\*/g, "").trim())}
          </h2>,
        );
      }
      // Date line
      else if (line.startsWith("📅")) {
        elements.push(
          <p
            key={key++}
            className="text-sm text-gray-500 mb-3 flex items-center gap-2"
          >
            <Clock size={14} className="text-blue-500" />
            {line.replace("📅", "").trim()}
          </p>,
        );
      }
      // Success message
      else if (line.startsWith("✅")) {
        elements.push(
          <p
            key={key++}
            className="text-sm text-emerald-600 font-medium mt-4 pt-3 border-t border-gray-100"
          >
            {line}
          </p>,
        );
      }
      // Normal paragraph
      else if (line.trim()) {
        elements.push(
          <p key={key++} className="text-gray-600 leading-relaxed mb-2">
            {parseInlineMarkdown(line)}
          </p>,
        );
      }
    }

    // Description lines under locations (indented)
    if (
      line.startsWith("   ") &&
      currentDayCard &&
      currentDayCard.locations.length > 0
    ) {
      const lastLoc =
        currentDayCard.locations[currentDayCard.locations.length - 1];
      lastLoc.description = (lastLoc.description || "") + line.trim() + " ";
    }
  }

  flushList(); // Flush any remaining list items

  // Push final day card if exists
  if (currentDayCard) {
    elements.push(renderDayCard(currentDayCard, key++));
  }

  return <div className="space-y-3">{elements}</div>;
};

/**
 * Render a day as a styled card
 */
const renderDayCard = (dayCard, key) => {
  return (
    <div
      key={key}
      className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden mb-4"
    >
      {/* Day Header */}
      <div className="px-4 py-2.5 bg-linear-to-r from-blue-50 to-indigo-50 border-b border-gray-100">
        <h3 className="text-sm font-bold text-gray-800">{dayCard.title}</h3>
      </div>

      {/* Locations */}
      <div className="p-3 space-y-2">
        {dayCard.locations.map((loc, idx) => (
          <div
            key={idx}
            className="flex gap-3 items-start p-2 rounded-lg hover:bg-gray-50 transition-colors"
          >
            {/* Index Dot */}
            <div className="w-6 h-6 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-bold shrink-0 mt-0.5">
              {loc.index}
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-medium text-gray-800 text-sm">
                  {loc.name}
                </span>
                {loc.time && (
                  <span className="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded-full">
                    {loc.time}
                  </span>
                )}
              </div>
              {loc.description && (
                <p className="text-xs text-gray-500 mt-1 line-clamp-2">
                  {loc.description.trim()}
                </p>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

/**
 * Parse inline markdown: **bold**
 */
const parseInlineMarkdown = (text) => {
  if (!text) return text;

  const parts = [];
  let key = 0;
  const boldRegex = /\*\*(.+?)\*\*/g;
  let lastIndex = 0;
  let match;

  while ((match = boldRegex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push(<span key={key++}>{text.slice(lastIndex, match.index)}</span>);
    }
    parts.push(
      <strong key={key++} className="font-semibold text-gray-900">
        {match[1]}
      </strong>,
    );
    lastIndex = match.index + match[0].length;
  }

  if (lastIndex < text.length) {
    parts.push(<span key={key++}>{text.slice(lastIndex)}</span>);
  }

  return parts.length > 0 ? parts : text;
};

export default React.memo(MessageContent);
